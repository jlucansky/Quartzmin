using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory
{
    [Serializable]
    public class ExecutionHistoryEntry
    {
        public string FireInstanceId { get; set; }
        public string SchedulerInstanceId { get; set; }
        public string SchedulerName { get; set; }
        public string Job { get; set; }
        public string Trigger { get; set; }
        public DateTime? ScheduledFireTimeUtc { get; set; }
        public DateTime ActualFireTimeUtc { get; set; }
        public bool Recovering { get; set; }
        public bool Vetoed { get; set; }
        public DateTime? FinishedTimeUtc { get; set; }
        public string ExceptionMessage { get; set; }
    }

    public interface IExecutionHistoryStore
    {
        string SchedulerName { get; set; }

        Task<ExecutionHistoryEntry> Get(string fireInstanceId);
        Task Save(ExecutionHistoryEntry entry);
        Task Purge();

        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit);

        Task<int> GetTotalJobsExecuted();
        Task<int> GetTotalJobsFailed();

        Task IncrementTotalJobsExecuted();
        Task IncrementTotalJobsFailed();
    }
}
