using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Quartz.Plugins.RecentHistory.Impl.SqlServer;
using Xunit;

namespace Quartz.Plugins.RecentHistory.Tests.Integration.Impl.SqlServer
{
    public class SqlServerExecutionHistoryStoreTests
    {
        protected string ConnectionString { get; set; }
        public string SchedulerName { get; set; }
        public string TablePrefix { get; set; }

        public SqlServerExecutionHistoryStoreTests()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            ConnectionString = config["Tests:ConnectionString"];
            SchedulerName = config["Tests:SchedulerName"];
            TablePrefix = config["Tests:TablePrefix"];
        }

        [Fact]
        public async void GetsEntry()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(1),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(2),
                ExceptionMessage = Guid.NewGuid().ToString(),
                Vetoed = true,
                Recovering= true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = Guid.NewGuid().ToString()
            };
            await store.Save(newEntry);
            
            var fetchedEntry = await store.Get(newEntry.FireInstanceId);

            Assert.NotNull(fetchedEntry);
            Assert.NotEqual(newEntry, fetchedEntry);
        }
        
        [Fact]
        public async void SavesEntry()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(1),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(2),
                ExceptionMessage = Guid.NewGuid().ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = Guid.NewGuid().ToString()
            };
            await store.Save(newEntry);

            var fetchedEntry = await store.Get(newEntry.FireInstanceId);

            Assert.NotNull(fetchedEntry);
            Assert.NotEqual(newEntry, fetchedEntry);

            Assert.Equal(ClearMilliseconds(newEntry.ActualFireTimeUtc), ClearMilliseconds(fetchedEntry.ActualFireTimeUtc));
            Assert.Equal(ClearMilliseconds(newEntry.ScheduledFireTimeUtc), ClearMilliseconds(fetchedEntry.ScheduledFireTimeUtc));
            Assert.Equal(ClearMilliseconds(newEntry.FinishedTimeUtc), ClearMilliseconds(fetchedEntry.FinishedTimeUtc));
            Assert.Equal(newEntry.ExceptionMessage, fetchedEntry.ExceptionMessage);
            Assert.Equal(newEntry.Vetoed, fetchedEntry.Vetoed);
            Assert.Equal(newEntry.Recovering, fetchedEntry.Recovering);
            Assert.Equal(newEntry.FireInstanceId, fetchedEntry.FireInstanceId);
            Assert.Equal(newEntry.Job, fetchedEntry.Job);
            Assert.Equal(newEntry.SchedulerInstanceId, fetchedEntry.SchedulerInstanceId);
            Assert.Equal(newEntry.SchedulerName, fetchedEntry.SchedulerName);
            Assert.Equal(newEntry.Trigger, fetchedEntry.Trigger);
        }

        [Fact]
        public async void PurgesEntry()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(100)),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(2),
                ExceptionMessage = Guid.NewGuid().ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = Guid.NewGuid().ToString()
            };
            await store.Save(newEntry);

            var fetchedEntryBeforePurge = await store.Get(newEntry.FireInstanceId);
            Assert.NotNull(fetchedEntryBeforePurge);
            Assert.NotEqual(newEntry, fetchedEntryBeforePurge);

            await store.Purge();

            var fetchedEntryAfterPurge = await store.Get(newEntry.FireInstanceId);
            Assert.Null(fetchedEntryAfterPurge);
        }

        [Fact]
        public async void FiltersLastOfEveryJob()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            var job1 = Guid.NewGuid().ToString();
            for (int i = 0; i < 5; i++)
            {
                var newEntry = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                    ScheduledFireTimeUtc = DateTime.UtcNow,
                    FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                    ExceptionMessage = i.ToString(),
                    Vetoed = true,
                    Recovering = true,
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = job1,
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = Guid.NewGuid().ToString()
                };

                await store.Save(newEntry);
            }

            var job2 = Guid.NewGuid().ToString();
            for (int i = 0; i < 5; i++)
            {
                var newEntry = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                    ScheduledFireTimeUtc = DateTime.UtcNow,
                    FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                    ExceptionMessage = i.ToString(),
                    Vetoed = true,
                    Recovering = true,
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = job2,
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = Guid.NewGuid().ToString()
                };

                await store.Save(newEntry);
            }

            var allEntries = await store.FilterLastOfEveryJob(3);

            var job1Entries = allEntries
                .Where(f => f.Job == job1)
                .ToList();
            Assert.Equal(3, job1Entries.Count);
            Assert.Equal("0", job1Entries[0].ExceptionMessage);
            Assert.Equal("1", job1Entries[1].ExceptionMessage);
            Assert.Equal("2", job1Entries[2].ExceptionMessage);

            var job2Entries = allEntries
                .Where(f => f.Job == job2)
                .ToList();
            Assert.Equal(3, job2Entries.Count);
            Assert.Equal("0", job2Entries[0].ExceptionMessage);
            Assert.Equal("1", job2Entries[1].ExceptionMessage);
            Assert.Equal("2", job2Entries[2].ExceptionMessage);
        }

        [Fact]
        public async void FiltersLastOfEveryTrigger()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            var trigger1 = Guid.NewGuid().ToString();
            for (int i = 0; i < 5; i++)
            {
                var newEntry = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                    ScheduledFireTimeUtc = DateTime.UtcNow,
                    FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                    ExceptionMessage = i.ToString(),
                    Vetoed = true,
                    Recovering = true,
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = Guid.NewGuid().ToString(),
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = trigger1
                };

                await store.Save(newEntry);
            }

            var trigger2 = Guid.NewGuid().ToString();
            for (int i = 0; i < 5; i++)
            {
                var newEntry = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                    ScheduledFireTimeUtc = DateTime.UtcNow,
                    FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                    ExceptionMessage = i.ToString(),
                    Vetoed = true,
                    Recovering = true,
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = Guid.NewGuid().ToString(),
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = trigger2
                };

                await store.Save(newEntry);
            }

            var allEntries = await store.FilterLastOfEveryTrigger(3);

            var trigger1Entries = allEntries
                .Where(f => f.Trigger == trigger1)
                .ToList();
            Assert.Equal(3, trigger1Entries.Count);
            Assert.Equal("0", trigger1Entries[0].ExceptionMessage);
            Assert.Equal("1", trigger1Entries[1].ExceptionMessage);
            Assert.Equal("2", trigger1Entries[2].ExceptionMessage);

            var trigger2Entries = allEntries
                .Where(f => f.Trigger == trigger2)
                .ToList();
            Assert.Equal(3, trigger2Entries.Count);
            Assert.Equal("0", trigger2Entries[0].ExceptionMessage);
            Assert.Equal("1", trigger2Entries[1].ExceptionMessage);
            Assert.Equal("2", trigger2Entries[2].ExceptionMessage);
        }

        [Fact]
        public async void FiltersLast()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            var trigger1 = Guid.NewGuid().ToString();
            for (int i = 0; i < 5; i++)
            {
                var newEntry = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(5 + 4 - i),
                    ScheduledFireTimeUtc = DateTime.UtcNow,
                    FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                    ExceptionMessage = i.ToString(),
                    Vetoed = true,
                    Recovering = true,
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = Guid.NewGuid().ToString(),
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = trigger1
                };

                await store.Save(newEntry);
            }

            var trigger2 = Guid.NewGuid().ToString();
            for (int i = 0; i < 5; i++)
            {
                var newEntry = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                    ScheduledFireTimeUtc = DateTime.UtcNow,
                    FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                    ExceptionMessage = i.ToString(),
                    Vetoed = true,
                    Recovering = true,
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = Guid.NewGuid().ToString(),
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = trigger2
                };

                await store.Save(newEntry);
            }

            var allEntries = await store.FilterLast(8);

            var trigger1Entries = allEntries
                .Where(f => f.Trigger == trigger1)
                .ToList();
            Assert.Equal(5, trigger1Entries.Count);
            Assert.Equal("0", trigger1Entries[0].ExceptionMessage);
            Assert.Equal("1", trigger1Entries[1].ExceptionMessage);
            Assert.Equal("2", trigger1Entries[2].ExceptionMessage);
            Assert.Equal("3", trigger1Entries[3].ExceptionMessage);
            Assert.Equal("4", trigger1Entries[4].ExceptionMessage);

            var trigger2Entries = allEntries
                .Where(f => f.Trigger == trigger2)
                .ToList();
            Assert.Equal(3, trigger2Entries.Count);
            Assert.Equal("0", trigger2Entries[0].ExceptionMessage);
            Assert.Equal("1", trigger2Entries[1].ExceptionMessage);
            Assert.Equal("2", trigger2Entries[2].ExceptionMessage);
        }

        [Fact]
        public async void IncrementTotalJobsExecuted()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();
            
            int preCount = await store.GetTotalJobsExecuted();

            await store.IncrementTotalJobsExecuted();

            int afterCount = await store.GetTotalJobsExecuted();

            Assert.Equal(preCount + 1, afterCount);
        }

        [Fact]
        public async void IncrementTotalJobsFailed()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            int preCount = await store.GetTotalJobsFailed();

            await store.IncrementTotalJobsFailed();

            int afterCount = await store.GetTotalJobsFailed();

            Assert.Equal(preCount + 1, afterCount);
        }

        [Fact]
        public async void GetTotalJobsExecuted()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            int count = await store.GetTotalJobsExecuted();

            Assert.Equal(0, count);
        }

        [Fact]
        public async void GetTotalJobsFailed()
        {
            var store = CreateStore();
            await store.ClearSchedulerData();

            int count = await store.GetTotalJobsFailed();

            Assert.Equal(0, count);
        }
        
        private DateTime? ClearMilliseconds(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return null;
            }

            var newDateTime = new DateTime(
                dateTime.Value.Year,
                dateTime.Value.Month,
                dateTime.Value.Day,
                dateTime.Value.Hour,
                dateTime.Value.Minute,
                dateTime.Value.Second, DateTimeKind.Utc);
            return newDateTime;
        }

        private SqlServerExecutionHistoryStore CreateStore()
        {
            var store = new SqlServerExecutionHistoryStore();
            store.SchedulerName = SchedulerName;
            store.ConnectionString = ConnectionString;
            store.TablePrefix = TablePrefix;
            return store;
        }
    }
}