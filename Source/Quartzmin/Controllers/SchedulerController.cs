using Quartz;
using Quartzmin.Helpers;
using Quartzmin.Models;
using Quartz.Plugins.RecentHistory;
using Quartz.Impl.Matchers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using Quartzmin.Security;

#region Target-Specific Directives
#if NETSTANDARD
using Microsoft.AspNetCore.Mvc;
#endif
#if NETFRAMEWORK
using System.Web.Http;
using IActionResult = System.Web.Http.IHttpActionResult;
#endif
#endregion

namespace Quartzmin.Controllers
{
    public class SchedulerController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
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

            return View(new
            {
                History = histogram,
                MetaData = metadata,
                RunningSince = metadata.RunningSince?.UtcDateTime.ToDefaultFormat() ?? "N / A",
                Environment.MachineName,
                Application = Environment.CommandLine,
                JobsCount = jobKeys.Count,
                TriggerCount = triggerKeys.Count,
                ExecutingJobs = currentlyExecutingJobs.Count,
                ExecutedJobs = executedJobs,
                FailedJobs = failedJobs?.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                JobGroups = pausedJobGroups,
                TriggerGroups = pausedTriggerGroups,
                HistoryEnabled = histStore != null,
            });
        }

        async Task<IEnumerable<object>> GetGroupPauseState(IEnumerable<string> groups, Func<string, Task<bool>> func)
        {
            var result = new List<object>();

            foreach (var name in groups.OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase))
                result.Add(new { Name = name, IsPaused = await func(name) });

            return result;
        }

        public class ActionArgs
        {
            public string Action { get; set; }
            public string Name { get; set; }
            public string Groups { get; set; } // trigger-groups | job-groups
        }

        [HttpPost, JsonErrorResponse]
        public async Task Action([FromBody] ActionArgs args)
        {
            switch (args.Action.ToLower())
            {
                case "shutdown":
                    RequireUserPermissions(UserPermissions.ControlScheduler);
                    await Scheduler.Shutdown();
                    break;
                case "standby":
                    RequireUserPermissions(UserPermissions.ControlScheduler);
                    await Scheduler.Standby();
                    break;
                case "start":
                    RequireUserPermissions(UserPermissions.ControlScheduler);
                    await Scheduler.Start();
                    break;
                case "pause":
                    if (string.IsNullOrEmpty(args.Name))
                    {
                        RequireUserPermissions(UserPermissions.ControlScheduler);
                        await Scheduler.PauseAll();
                    }
                    else
                    {
                        if (args.Groups == "trigger-groups")
                        {
                            RequireUserPermissions(UserPermissions.ControlTriggers);
                            await Scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals(args.Name));
                        }
                        else if (args.Groups == "job-groups")
                        {
                            RequireUserPermissions(UserPermissions.ControlJobs);
                            await Scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(args.Name));
                        }
                        else
                            throw new InvalidOperationException("Invalid groups: " + args.Groups);
                    }
                    break;
                case "resume":
                    if (string.IsNullOrEmpty(args.Name))
                    {
                        RequireUserPermissions(UserPermissions.ControlScheduler);
                        await Scheduler.ResumeAll();
                    }
                    else
                    {
                        if (args.Groups == "trigger-groups")
                        {
                            RequireUserPermissions(UserPermissions.ControlTriggers);
                            await Scheduler.ResumeTriggers(GroupMatcher<TriggerKey>.GroupEquals(args.Name));
                        }
                        else if (args.Groups == "job-groups")
                        {
                            RequireUserPermissions(UserPermissions.ControlJobs);
                            await Scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(args.Name));
                        }
                        else
                            throw new InvalidOperationException("Invalid groups: " + args.Groups);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid action: " + args.Action);
            }

            void RequireUserPermissions(params UserPermissions[] userPermissions)
            {
                if (!UserHasPermissions(userPermissions))
                {
                    throw new UnauthorizedAccessException("Missing required permissions to perform this action");
                }
            }
        }
    }
}
