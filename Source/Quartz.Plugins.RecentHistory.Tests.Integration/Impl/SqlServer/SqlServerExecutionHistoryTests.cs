using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory.Impl.SqlServer;
using Xunit;

namespace Quartz.Plugins.RecentHistory.Tests.Integration.Impl.SqlServer
{
    public class SqlServerExecutionHistoryTests : IDisposable
    {
        protected string ConnectionString { get; set; }
        public string SchedulerName { get; set; }
        public string TablePrefix { get; set; }

        public SqlServerExecutionHistoryTests()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            ConnectionString = config["Tests:ConnectionString"];
            SchedulerName = config["Tests:SchedulerName"];
            TablePrefix = config["Tests:TablePrefix"];
        }

        [Fact]
        public async void SavesJob()
        {
            var sched = await SetupScheduler();
            try
            {
                var jobName = "JB" + Guid.NewGuid();
                await ScheduleTestJob(sched, jobName: jobName, jobGroup: "TEST");

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) => { resetEvent.Set(); };

                var store = CreateStore();
                await store.ClearSchedulerData();

                var lastJob = (await store.FilterLast(1)).FirstOrDefault();
                
                await sched.Start();

                resetEvent.Wait(3 * 1000);
                Assert.True(resetEvent.IsSet);

                var newLastJob = (await store.FilterLast(1)).FirstOrDefault();

                Assert.NotNull(newLastJob);
                if (lastJob != null)
                {
                    Assert.NotEqual(lastJob.FireInstanceId, newLastJob.FireInstanceId);
                }

                Assert.Equal($"TEST.{jobName}", newLastJob.Job);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }

        [Fact]
        public async void Purges()
        {
            var sched = await SetupScheduler();
            try
            {
                var jobName = "JB" + Guid.NewGuid();
                await ScheduleTestJob(sched, jobName: jobName, jobGroup: "TEST");

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) => { resetEvent.Set(); };

                var store = CreateStore();
                await store.ClearSchedulerData();

                var entryToBePurged = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(100)),
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = "TEST.PurgeMe",
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = Guid.NewGuid().ToString()
                };
                await store.Save(entryToBePurged);

                var lastJobs = await store.FilterLast(2);
                Assert.Contains(lastJobs, j => j.FireInstanceId == entryToBePurged.FireInstanceId);

                await sched.Start();

                resetEvent.Wait(3 * 1000);
                Assert.True(resetEvent.IsSet);

                var newLastJobs = await store.FilterLast(2);
                Assert.DoesNotContain(newLastJobs, j => j.FireInstanceId == entryToBePurged.FireInstanceId);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }
        
        [Fact]
        public async void IncrementsTotalJobsExecuted()
        {
            var sched = await SetupScheduler();
            try
            {
                await ScheduleTestJob(sched);

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) =>
                {
                    resetEvent.Set();
                };

                var store = CreateStore();
                await store.ClearSchedulerData();

                int currentCount = await store.GetTotalJobsExecuted();

                await sched.Start();

                resetEvent.Wait(3 * 1000);
                Assert.True(resetEvent.IsSet);

                int newCount = await store.GetTotalJobsExecuted();
                Assert.Equal(currentCount + 1, newCount);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }

        [Fact]
        public async void IncrementsTotalJobsFailed()
        {
            var sched = await SetupScheduler();
            try
            {
                await ScheduleTestJob(sched);

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) =>
                {
                    if (e != null)
                    {
                        resetEvent.Set();
                    }
                };
                TestJob.Callback = c => throw new Exception("FAILURE!");

                var store = CreateStore();
                await store.ClearSchedulerData();

                int currentCount = await store.GetTotalJobsFailed();

                await sched.Start();

                resetEvent.Wait(3 * 1000);
                Assert.True(resetEvent.IsSet);

                int newCount = await store.GetTotalJobsFailed();
                Assert.Equal(currentCount + 1, newCount);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }

        private async Task<IScheduler> SetupScheduler()
        {
            var properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = SchedulerName,
                ["quartz.plugin.recentHistory.type"] = typeof(SqlServerExecutionHistoryPlugin).AssemblyQualifiedName,
                ["quartz.plugin.recentHistory.connectionString"] = ConnectionString
            };

            var sf = new StdSchedulerFactory(properties);
            var sched = await sf.GetScheduler();
            return sched;
        }

        private async Task ScheduleTestJob(IScheduler sched,
            string jobName = "job1",
            string jobGroup = "group1",
            string triggerName = "trigger1",
            string triggerGroup = "group1")
        {
            var job = JobBuilder.Create<TestJob>()
                .WithIdentity(jobName, jobGroup)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerName, triggerGroup)
                .StartNow()
                .Build();

            await sched.ScheduleJob(job, trigger);
        }

        private SqlServerExecutionHistoryStore CreateStore()
        {
            var store = new SqlServerExecutionHistoryStore();
            store.SchedulerName = SchedulerName;
            store.ConnectionString = ConnectionString;
            store.TablePrefix = TablePrefix;
            return store;
        }

        private void ResetCallbacks()
        {
            TestJobListener.JobWasExecutedCallback = null;
            TestJob.Callback = null;
        }

        public void Dispose()
        {
            ResetCallbacks();
        }
    }
}
