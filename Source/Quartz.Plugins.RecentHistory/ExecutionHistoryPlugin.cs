using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory
{
    public class ExecutionHistoryPlugin : ISchedulerPlugin, IJobListener
    {
        IScheduler _scheduler;
        IExecutionHistoryStore _store;

        public string Name { get; set; }
        public Type ExecutionHistoryStoreType { get; set; }

        public Task Initialize(string pluginName, IScheduler scheduler, CancellationToken cancellationToken = default(CancellationToken))
        {
            Name = pluginName;
            _scheduler = scheduler;
            _scheduler.ListenerManager.AddJobListener(this, EverythingMatcher<JobKey>.AllJobs());
            
            return Task.FromResult(0);
        }

        public async Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            _store = _scheduler.Context.GetExecutionHistoryStore();

            if (_store == null)
            {
                if (ExecutionHistoryStoreType != null)
                    _store = (IExecutionHistoryStore)Activator.CreateInstance(ExecutionHistoryStoreType);

                if (_store == null)
                    throw new Exception(nameof(ExecutionHistoryStoreType) + " is not set.");

                _scheduler.Context.SetExecutionHistoryStore(_store);
            }

            _store.SchedulerName = _scheduler.SchedulerName;

            await _store.Purge();
        }

        public Task Shutdown(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entry = new ExecutionHistoryEntry()
            {
                FireInstanceId = context.FireInstanceId,
                SchedulerInstanceId = context.Scheduler.SchedulerInstanceId,
                SchedulerName = context.Scheduler.SchedulerName,
                ActualFireTimeUtc = context.FireTimeUtc.UtcDateTime,
                ScheduledFireTimeUtc = context.ScheduledFireTimeUtc?.UtcDateTime,
                Recovering = context.Recovering,
                Job = context.JobDetail.Key.ToString(),
                Trigger = context.Trigger.Key.ToString(),
            };
            await _store.Save(entry);
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entry = await _store.Get(context.FireInstanceId);
            if (entry != null)
            {
                entry.FinishedTimeUtc = DateTime.UtcNow;
                entry.ExceptionMessage = jobException?.GetBaseException()?.Message;
                await _store.Save(entry);
            }
            if (jobException == null)
                await _store.IncrementTotalJobsExecuted();
            else
                await _store.IncrementTotalJobsFailed();
        }

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entry = await _store.Get(context.FireInstanceId);
            if (entry != null)
            {
                entry.Vetoed = true;
                await _store.Save(entry);
            }
        }
    }
}
