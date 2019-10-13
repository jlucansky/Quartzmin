using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using C = Quartz.Plugins.RecentHistory.Impl.SqlServer.SqlServerConstants;

namespace Quartz.Plugins.RecentHistory.Impl.SqlServer
{
    public class SqlServerExecutionHistoryStore : IExecutionHistoryStore
    {
        private DateTime _nextPurgeTime = DateTime.UtcNow;

        public string SchedulerName { get; set; }
        public string ConnectionString { get; set; }
        public string TablePrefix { get; set; }
        public int PurgeIntervalInMinutes { get; set; }
        public int EntryTTLInMinutes { get; set; }

        public async Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            if (fireInstanceId == null) throw new ArgumentNullException(nameof(fireInstanceId));

            string query =
                $"SELECT * FROM {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                $"WHERE {C.ColumnFireInstanceId} = @FireInstanceId";

            var entries = await ExecuteExecutionHistoryEntryQuery(query, c => c.Parameters.AddWithValue("@FireInstanceId", fireInstanceId));
            return entries.FirstOrDefault();
        }

        public async Task Save(ExecutionHistoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (_nextPurgeTime < DateTime.UtcNow)
            {
                _nextPurgeTime = DateTime.UtcNow.AddMinutes(PurgeIntervalInMinutes);
                await Purge();
            }

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string query =
                    $"MERGE {GetTableName(C.TableExecutionHistoryEntries)} AS [Target] \n" +
                    $"USING (SELECT @FireInstanceId AS {C.ColumnFireInstanceId}) AS [Source] \n" +
                    $"ON [Source].{C.ColumnFireInstanceId} = [Target].{C.ColumnFireInstanceId} \n" +
                    $"WHEN MATCHED THEN UPDATE SET \n" +
                    $"{C.ColumnFireInstanceId} = @FireInstanceId, {C.ColumnSchedulerInstanceId} = @SchedulerInstanceId, {C.ColumnSchedulerName} = @SchedulerName, \n" +
                    $"{C.ColumnJob} = @Job, {C.ColumnTrigger} = @Trigger, {C.ColumnScheduledFireTimeUtc} = @ScheduledFireTimeUtc, {C.ColumnActualFireTimeUtc} = @ActualFireTimeUtc, \n" +
                    $"{C.ColumnRecovering} = @Recovering, {C.ColumnVetoed} = @Vetoed, {C.ColumnFinishedTimeUtc} = @FinishedTimeUtc, {C.ColumnExceptionMessage} = @ExceptionMessage \n" +
                    $"WHEN NOT MATCHED THEN INSERT \n" +
                    $"({C.ColumnFireInstanceId}, {C.ColumnSchedulerInstanceId}, {C.ColumnSchedulerName}, \n" +
                    $"{C.ColumnJob}, {C.ColumnTrigger}, {C.ColumnScheduledFireTimeUtc}, {C.ColumnActualFireTimeUtc}, \n" +
                    $"{C.ColumnRecovering}, {C.ColumnVetoed}, {C.ColumnFinishedTimeUtc}, {C.ColumnExceptionMessage}) \n" +
                    $"VALUES (@FireInstanceId, @SchedulerInstanceId, @SchedulerName, @Job, @Trigger, @ScheduledFireTimeUtc, \n" +
                    $"@ActualFireTimeUtc, @Recovering, @Vetoed, @FinishedTimeUtc, @ExceptionMessage);";

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@FireInstanceId", entry.FireInstanceId);
                    sqlCommand.Parameters.AddWithValue("@SchedulerInstanceId", entry.SchedulerInstanceId);
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", entry.SchedulerName);
                    sqlCommand.Parameters.AddWithValue("@Job", entry.Job);
                    sqlCommand.Parameters.AddWithValue("@Trigger", entry.Trigger);
                    sqlCommand.Parameters.AddWithValue("@ScheduledFireTimeUtc", entry.ScheduledFireTimeUtc != null ? (object)entry.ScheduledFireTimeUtc : DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@ActualFireTimeUtc", entry.ActualFireTimeUtc);
                    sqlCommand.Parameters.AddWithValue("@Recovering", entry.Recovering);
                    sqlCommand.Parameters.AddWithValue("@Vetoed", entry.Vetoed);
                    sqlCommand.Parameters.AddWithValue("@FinishedTimeUtc", entry.FinishedTimeUtc != null ? (object)entry.FinishedTimeUtc : DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@ExceptionMessage", entry.ExceptionMessage != null ? (object)entry.ExceptionMessage : DBNull.Value);

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task Purge()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string commandText = $"DELETE FROM {GetTableName(C.TableExecutionHistoryEntries)} WHERE {C.ColumnActualFireTimeUtc} < @PurgeThreshold";
                using (var sqlCommand = new SqlCommand(commandText, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@PurgeThreshold", DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(EntryTTLInMinutes)));

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            return await FilterLastOf(C.ColumnJob, limitPerJob);
        }

        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            return await FilterLastOf(C.ColumnTrigger, limitPerTrigger);
        }

        protected async Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOf(string columnName, int limit)
        {
            string query =
                $"WITH SELECTION AS ( \n" +
                $"	SELECT *, \n" +
                $"	ROW_NUMBER() OVER(PARTITION BY {columnName} ORDER BY {C.ColumnActualFireTimeUtc} DESC) AS ROW_KEY \n" +
                $"	FROM {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                $"	WHERE {C.ColumnSchedulerName} = @SchedulerName \n" +
                $") \n" +
                $"SELECT * \n" +
                $"FROM SELECTION \n" +
                $"WHERE ROW_KEY <= {limit}";

            return await ExecuteExecutionHistoryEntryQuery(query, c => c.Parameters.AddWithValue("@SchedulerName", SchedulerName));
        }
        
        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            string query =
                $"SELECT TOP {limit} * FROM {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                $"WHERE {C.ColumnSchedulerName} = @SchedulerName \n" +
                $"ORDER BY {C.ColumnActualFireTimeUtc} DESC";

            return await ExecuteExecutionHistoryEntryQuery(query, c => c.Parameters.AddWithValue("@SchedulerName", SchedulerName));
        }

        public async Task<int> GetTotalJobsExecuted()
        {
            try
            {
                return (int)await GetStatValue(C.StatTotalJobsExecuted);
            }
            catch (OverflowException)
            {
                /*  should actually log here, but Quartz does not expose its
                    logging facilities to external plugins */
                return -1;
            }
        }

        public async Task<int> GetTotalJobsFailed()
        {
            try
            {
                return (int)await GetStatValue(C.StatTotalJobsFailed);
            }
            catch (OverflowException)
            {
                /*  should actually log here, but Quartz does not expose its
                    logging facilities to external plugins */
                return -1;
            }
        }

        public async Task IncrementTotalJobsExecuted()
        {
            await IncrementStatValue(C.StatTotalJobsExecuted);
        }

        public async Task IncrementTotalJobsFailed()
        {
            await IncrementStatValue(C.StatTotalJobsFailed);
        }

        public async Task ClearSchedulerData()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string commandText = 
                    $"DELETE FROM {GetTableName(C.TableExecutionHistoryEntries)} WHERE {C.ColumnSchedulerName} = @SchedulerName;\n" +
                    $"DELETE FROM {GetTableName(C.TableExecutionHistoryStats)} WHERE {C.ColumnSchedulerName} = @SchedulerName;";
                using (var sqlCommand = new SqlCommand(commandText, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", SchedulerName);

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        protected string GetTableName(string tableNameWithoutPrefix)
        {
            if (tableNameWithoutPrefix == null) throw new ArgumentNullException(nameof(tableNameWithoutPrefix));

            return $"{TablePrefix}{tableNameWithoutPrefix}";
        }

        protected async Task<List<ExecutionHistoryEntry>> ExecuteExecutionHistoryEntryQuery(string query, Action<SqlCommand> sqlCommandModifier = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommandModifier?.Invoke(sqlCommand);

                    using (var sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        var entries = new List<ExecutionHistoryEntry>();

                        while (await sqlDataReader.ReadAsync())
                        {
                            var entry = new ExecutionHistoryEntry();
                            await HydrateExecutionHistoryEntry(sqlDataReader, entry);
                            entries.Add(entry);
                        }

                        return entries;
                    }
                }
            }
        }

        protected async Task<long> GetStatValue(string statName)
        {
            if (statName == null) throw new ArgumentNullException(nameof(statName));

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string query =
                    $"SELECT {C.ColumnStatValue} FROM {GetTableName(C.TableExecutionHistoryStats)} \n" +
                    $"WHERE {C.ColumnStatName} = @StatName AND {C.ColumnSchedulerName} = @SchedulerName";

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", SchedulerName);
                    sqlCommand.Parameters.AddWithValue("@StatName", statName);

                    var scalar = await sqlCommand.ExecuteScalarAsync();
                    if (scalar != null)
                    {
                        return (long)scalar;
                    }

                    return 0;
                }
            }
        }

        protected async Task IncrementStatValue(string statName)
        {
            if (statName == null) throw new ArgumentNullException(nameof(statName));

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string query =
                    $"MERGE {GetTableName(C.TableExecutionHistoryStats)} AS [Target] \n" +
                    $"USING (SELECT @StatName AS {C.ColumnStatName}, @SchedulerName AS {C.ColumnSchedulerName}) AS [Source] \n" +
                    $"ON [Source].{C.ColumnStatName} = [Target].{C.ColumnStatName} AND [Source].{C.ColumnSchedulerName} = [Target].{C.ColumnSchedulerName} \n" +
                    $"WHEN MATCHED THEN UPDATE SET \n" +
                    $"{C.ColumnStatValue} = {C.ColumnStatValue} + 1 \n" +
                    $"WHEN NOT MATCHED THEN INSERT \n" +
                    $"({C.ColumnSchedulerName}, {C.ColumnStatName}, {C.ColumnStatValue}) \n" +
                    $"VALUES (@SchedulerName, @StatName, 1);";

                using (var sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", SchedulerName);
                    sqlCommand.Parameters.AddWithValue("@StatName", statName);

                    try
                    {
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                    catch (SqlException e) when (e.Number == 8115) // SQL overflow exception
                    {
                        /*  should actually log here, but Quartz does not expose its
                            logging facilities to external plugins */
                    }
                }
            }
        }

        private async Task HydrateExecutionHistoryEntry(SqlDataReader sqlDataReader, ExecutionHistoryEntry entry)
        {
            var r = sqlDataReader;

            entry.ActualFireTimeUtc = r.GetDateTime(r.GetOrdinal(C.ColumnActualFireTimeUtc));
            entry.ExceptionMessage = await r.IsDBNullAsync(r.GetOrdinal(C.ColumnExceptionMessage)) ?
                null : r.GetString(r.GetOrdinal(C.ColumnExceptionMessage));
            entry.FinishedTimeUtc = await r.IsDBNullAsync(r.GetOrdinal(C.ColumnFinishedTimeUtc)) ?
                (DateTime?)null : r.GetDateTime(r.GetOrdinal(C.ColumnFinishedTimeUtc));
            entry.FireInstanceId = r.GetString(r.GetOrdinal(C.ColumnFireInstanceId));
            entry.Job = r.GetString(r.GetOrdinal(C.ColumnJob));
            entry.Recovering = r.GetBoolean(r.GetOrdinal(C.ColumnRecovering));
            entry.ScheduledFireTimeUtc = await r.IsDBNullAsync(r.GetOrdinal(C.ColumnScheduledFireTimeUtc)) ?
                (DateTime?)null : r.GetDateTime(r.GetOrdinal(C.ColumnScheduledFireTimeUtc));
            entry.SchedulerInstanceId = r.GetString(r.GetOrdinal(C.ColumnSchedulerInstanceId));
            entry.SchedulerName = r.GetString(r.GetOrdinal(C.ColumnSchedulerName));
            entry.Trigger = r.GetString(r.GetOrdinal(C.ColumnTrigger));
            entry.Vetoed = r.GetBoolean(r.GetOrdinal(C.ColumnVetoed));
        }
    }
}
