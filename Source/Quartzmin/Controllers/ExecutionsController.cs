using Quartz;
using Quartzmin.Helpers;
using Quartzmin.Models;
using Quartz.Plugins.RecentHistory;
using Quartz.Impl.Matchers;
using System;
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
    public class ExecutionsController : PageControllerBase
    {
        [HttpGet]
        [AuthorizeUser(UserPermissions.ViewExecutions)]
        public async Task<IActionResult> Index()
        {
            var currentlyExecutingJobs = await Scheduler.GetCurrentlyExecutingJobs();

            var list = new List<object>();

            foreach (var exec in currentlyExecutingJobs)
            {
                list.Add(new
                {
                    Id = exec.FireInstanceId,
                    JobGroup = exec.JobDetail.Key.Group,
                    JobName = exec.JobDetail.Key.Name,
                    TriggerGroup = exec.Trigger.Key.Group,
                    TriggerName = exec.Trigger.Key.Name,
                    ScheduledFireTime = exec.ScheduledFireTimeUtc?.UtcDateTime.ToDefaultFormat(),
                    ActualFireTime = exec.FireTimeUtc.UtcDateTime.ToDefaultFormat(),
                    RunTime = exec.JobRunTime.ToString("hh\\:mm\\:ss")
                });
            }

            return View(list);
        }

        public class InterruptArgs
        {
            public string Id { get; set; }
        }

        [HttpPost, JsonErrorResponse]
        [AuthorizeUser(UserPermissions.InterruptExecutions)]
        public async Task<IActionResult> Interrupt([FromBody] InterruptArgs args)
        {
            if (!await Scheduler.Interrupt(args.Id))
                throw new InvalidOperationException("Cannot interrupt execution " + args.Id);

            return NoContent();
        }
    }
}
