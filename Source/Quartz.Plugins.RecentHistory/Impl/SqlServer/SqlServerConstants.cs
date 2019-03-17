namespace Quartz.Plugins.RecentHistory.Impl.SqlServer
{
    internal static class SqlServerConstants
    {
        internal const string TableExecutionHistoryEntries = "EXECUTION_HISTORY_ENTRIES";
        internal const string TableExecutionHistoryStats = "EXECUTION_HISTORY_STATS";

        // TableExecutionHistoryEntries columns:
        internal const string ColumnFireInstanceId = "FIRE_INSTANCE_ID";
        internal const string ColumnSchedulerInstanceId = "SCHEDULER_INSTANCE_ID";
        internal const string ColumnSchedulerName = "SCHED_NAME";
        internal const string ColumnJob = "JOB_NAME";
        internal const string ColumnTrigger = "TRIGGER_NAME";
        internal const string ColumnScheduledFireTimeUtc = "SCHEDULED_FIRE_TIME_UTC";
        internal const string ColumnActualFireTimeUtc = "ACTUAL_FIRE_TIME_UTC";
        internal const string ColumnRecovering = "RECOVERING";
        internal const string ColumnVetoed = "VETOED";
        internal const string ColumnFinishedTimeUtc = "FINISHED_TIME_UTC";
        internal const string ColumnExceptionMessage = "EXCEPTION_MESSAGE";

        // TableExecutionHistoryStats columns:
        // internal const string ColumnSchedulerName = "SCHED_NAME";
        internal const string ColumnStatName = "STAT_NAME";
        internal const string ColumnStatValue = "STAT_VALUE";

        // Stat names:
        internal const string StatTotalJobsExecuted = "TOTAL_JOBS_EXECUTED";
        internal const string StatTotalJobsFailed = "TOTAL_JOBS_FAILED";
    }
}
