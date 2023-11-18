using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory;
using Quartzmin.Models;

namespace Quartzmin.Helpers
{
    public class ScheduleInfoHelper
    {
        public async Task<object> GetScheduleInfo(IScheduler Scheduler)
        {
            var histStore = Scheduler.Context.GetExecutionHistoryStore();
            var metadata = await Scheduler.GetMetaData();
            var jobKeys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var triggerKeys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var currentlyExecutingJobs = await Scheduler.GetCurrentlyExecutingJobs();
            IEnumerable<object> pausedJobGroups = null;
            IEnumerable<object> pausedTriggerGroups = null;
            IEnumerable<ExecutionHistoryEntry> execHistory = null;

            try
            {
                pausedJobGroups = await GetGroupPauseState(await Scheduler.GetJobGroupNames(), async x => await Scheduler.IsJobGroupPaused(x));
            } catch (NotImplementedException) { }

            try {
                pausedTriggerGroups = await GetGroupPauseState(await Scheduler.GetTriggerGroupNames(), async x => await Scheduler.IsTriggerGroupPaused(x));
            } catch (NotImplementedException) { }

            int? failedJobs = null;
            int executedJobs = metadata.NumberOfJobsExecuted;
            
            if (histStore != null)
            {
                execHistory = await histStore?.FilterLast(10);
                executedJobs = await histStore?.GetTotalJobsExecuted();
                failedJobs = await histStore?.GetTotalJobsFailed();
            }

            var histogram = execHistory.ToHistogram(detailed: true) ?? Histogram.CreateEmpty();

            histogram.BarWidth = 14;

            return new
            {
                History = histogram,
                //MetaData = metadata,
                RunningSince = metadata.RunningSince != null ? metadata.RunningSince.Value.UtcDateTime.ToDefaultFormat() + " UTC" : "N / A",
                Environment.MachineName,
                Application = Environment.CommandLine,
                JobsCount = jobKeys.Count,
                TriggerCount = triggerKeys.Count,
                ExecutingJobs = currentlyExecutingJobs.Count,
                ExecutedJobs = executedJobs,
                FailedJobs = failedJobs?.ToString(CultureInfo.InvariantCulture) ?? "N / A",
                //JobGroups = pausedJobGroups,
                //TriggerGroups = pausedTriggerGroups,
                HistoryEnabled = histStore != null,
            };
        }

        async Task<IEnumerable<object>> GetGroupPauseState(IEnumerable<string> groups, Func<string, Task<bool>> func)
        {
            var result = new List<object>();

            foreach (var name in groups.OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase))
                result.Add(new { Name = name, IsPaused = await func(name) });

            return result;
        }
    }
}