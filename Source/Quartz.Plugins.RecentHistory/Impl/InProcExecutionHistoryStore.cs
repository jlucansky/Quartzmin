using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.Impl
{
    public class InProcExecutionHistoryStore : IExecutionHistoryStore
    {
        public string SchedulerName { get; set; }

        Dictionary<string, ExecutionHistoryEntry> _data = new Dictionary<string, ExecutionHistoryEntry>();

        DateTime _nextPurgeTime = DateTime.UtcNow;
        int _updatesFromLastPurge;

        int _totalJobsExecuted = 0, _totalJobsFailed = 0;

        public Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            lock (_data)
            {
                if (_data.TryGetValue(fireInstanceId, out var entry))
                    return Task.FromResult(entry);
                else
                    return Task.FromResult<ExecutionHistoryEntry>(null); ;
            }
        }

        public async Task Purge()
        {
            var ids = new HashSet<string>((await FilterLastOfEveryTrigger(10)).Select(x => x.FireInstanceId));

            lock (_data)
            {
                foreach (var key in _data.Keys.ToArray())
                {
                    if (!ids.Contains(key))
                        _data.Remove(key);
                }
            }
        }

        public async Task Save(ExecutionHistoryEntry entry)
        {
            _updatesFromLastPurge++;

            if (_updatesFromLastPurge >= 10 || _nextPurgeTime < DateTime.UtcNow)
            {
                _nextPurgeTime = DateTime.UtcNow.AddMinutes(1);
                _updatesFromLastPurge = 0;
                await Purge();
            }

            lock (_data)
            {
                _data[entry.FireInstanceId] = entry;
            }
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            lock (_data)
            {
                IEnumerable<ExecutionHistoryEntry> result = _data.Values
                    .Where(x => x.SchedulerName == SchedulerName)
                    .GroupBy(x => x.Job)
                    .Select(x => x.OrderByDescending(y => y.ActualFireTimeUtc).Take(limitPerJob).Reverse())
                    .SelectMany(x => x).ToArray();
                return Task.FromResult(result);
            }
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            lock (_data)
            {
                IEnumerable<ExecutionHistoryEntry> result = _data.Values
                    .Where(x => x.SchedulerName == SchedulerName)
                    .GroupBy(x => x.Trigger)
                    .Select(x => x.OrderByDescending(y => y.ActualFireTimeUtc).Take(limitPerTrigger).Reverse())
                    .SelectMany(x => x).ToArray();
                return Task.FromResult(result);
            }
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            lock (_data)
            {
                IEnumerable<ExecutionHistoryEntry> result = _data.Values
                    .Where(x => x.SchedulerName == SchedulerName)
                    .OrderByDescending(y => y.ActualFireTimeUtc).Take(limit).Reverse().ToArray();
                return Task.FromResult(result);
            }
        }

        public Task<int> GetTotalJobsExecuted()
        {
            return Task.FromResult(_totalJobsExecuted);
        }
        public Task<int> GetTotalJobsFailed()
        {
            return Task.FromResult(_totalJobsFailed);
        }

        public Task IncrementTotalJobsExecuted()
        {
            Interlocked.Increment(ref _totalJobsExecuted);
            return Task.FromResult(0);
        }

        public Task IncrementTotalJobsFailed()
        {
            Interlocked.Increment(ref _totalJobsFailed);
            return Task.FromResult(0);
        }
    }
}
