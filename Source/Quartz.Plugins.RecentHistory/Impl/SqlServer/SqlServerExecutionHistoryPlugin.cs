using System;

namespace Quartz.Plugins.RecentHistory.Impl.SqlServer
{
    public class SqlServerExecutionHistoryPlugin : ExecutionHistoryPlugin
    {
        public string ConnectionString { get; set; }
        public string TablePrefix { get; set; } = "QRTZ_";
        public int PurgeIntervalInMinutes { get; set; } = 1;
        public int EntryTTLInMinutes { get; set; } = 2;

        protected override IExecutionHistoryStore CreateExecutionHistoryStore()
        {
            if (StoreType != null && StoreType != typeof(SqlServerExecutionHistoryStore))
            {
                throw new InvalidOperationException($"{nameof(SqlServerExecutionHistoryPlugin)} is only compatible with the {nameof(SqlServerExecutionHistoryStore)} store type");
            }

            var store = new SqlServerExecutionHistoryStore
            {
                ConnectionString = ConnectionString,
                TablePrefix = TablePrefix,
                PurgeIntervalInMinutes = PurgeIntervalInMinutes,
                EntryTTLInMinutes = EntryTTLInMinutes
            };
            return store;
        }
    }
}
