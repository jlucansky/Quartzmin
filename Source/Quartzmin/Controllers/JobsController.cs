using Quartz;
using Quartz.Impl.Matchers;
using Quartzmin.Helpers;
using Quartzmin.Models;
using Quartz.Plugins.RecentHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class JobsController : PageControllerBase
    {
        [HttpGet]
        [AuthorizeUser(UserPermissions.ViewJobs)]
        public async Task<IActionResult> Index()
        {
            var keys = (await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup())).OrderBy(x => x.ToString());
            var list = new List<JobListItem>();
            var knownTypes = new List<string>();

            foreach (var key in keys)
            {
                var detail = await GetJobDetail(key);
                var item = new JobListItem()
                {
                    Concurrent = !detail.ConcurrentExecutionDisallowed,
                    Persist = detail.PersistJobDataAfterExecution,
                    Recovery = detail.RequestsRecovery,
                    JobName = key.Name,
                    Group = key.Group,
                    Type = detail.JobType.FullName,
                    History = Histogram.Empty,
                    Description = detail.Description,
                };
                knownTypes.Add(detail.JobType.RemoveAssemblyDetails());
                list.Add(item);
            }

            Services.Cache.UpdateJobTypes(knownTypes);

            ViewBag.Groups = (await Scheduler.GetJobGroupNames()).GroupArray();

            return View(list);
        }

        [HttpGet]
        [AuthorizeUser(UserPermissions.CreateNewJobs)]
        public async Task<IActionResult> New()
        {
            var job = new JobPropertiesViewModel() { IsNew = true };
            var jobDataMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

            job.GroupList = (await Scheduler.GetJobGroupNames()).GroupArray();
            job.Group = SchedulerConstants.DefaultGroup;
            job.TypeList = Services.Cache.JobTypes;

            return View("Edit", new JobViewModel() { Job = job, DataMap = jobDataMap });
        }

        [HttpGet]
        [AuthorizeUser(UserPermissions.TriggerJobs)]
        public async Task<IActionResult> Trigger(string name, string group)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobKey = JobKey.Create(name, group);
            var job = await GetJobDetail(jobKey);
            var jobDataMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

            ViewBag.JobName = name;
            ViewBag.Group = group;

            jobDataMap.Items.AddRange(job.GetJobDataMapModel(Services));

            return View(jobDataMap);
        }

        [HttpPost, ActionName("Trigger"), JsonErrorResponse]
        [AuthorizeUser(UserPermissions.TriggerJobs)]
        public async Task<IActionResult> PostTrigger(string name, string group)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel(Services);

            var result = new ValidationResult();

            ModelValidator.Validate(jobDataMap, result.Errors);

            if (result.Success)
            {
                await Scheduler.TriggerJob(JobKey.Create(name, group), jobDataMap.GetQuartzJobDataMap());
            }

            return Json(result);
        }

        [HttpGet]
        [AuthorizeUser(UserPermissions.ViewJobs)]
        public async Task<IActionResult> Edit(string name, string group, bool clone = false)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobKey = JobKey.Create(name, group);
            var job = await GetJobDetail(jobKey);

            var jobModel = new JobPropertiesViewModel() { };
            var jobDataMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

            jobModel.IsNew = clone;
            jobModel.IsCopy = clone;
            jobModel.JobName = name;
            jobModel.Group = group;
            jobModel.GroupList = (await Scheduler.GetJobGroupNames()).GroupArray();

            jobModel.Type = job.JobType.RemoveAssemblyDetails();
            jobModel.TypeList = Services.Cache.JobTypes;

            jobModel.Description = job.Description;
            jobModel.Recovery = job.RequestsRecovery;

            if (clone)
                jobModel.JobName += " - Copy";

            jobDataMap.Items.AddRange(job.GetJobDataMapModel(Services));

            return View("Edit", new JobViewModel() { Job = jobModel, DataMap = jobDataMap });
        }

        private async Task<IJobDetail> GetJobDetail(JobKey key)
        {
            var job = await Scheduler.GetJobDetail(key);

            if (job == null)
                throw new InvalidOperationException("Job " + key + " not found.");

            return job;
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Save([FromForm] JobViewModel model, bool trigger)
        {
            if ((model.Job.IsNew && !UserHasPermissions(UserPermissions.CreateNewJobs)) ||
                !UserHasPermissions(UserPermissions.EditJobs))
            {
                return Unauthorized();
            }

            var jobModel = model.Job;
            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel(Services);

            var result = new ValidationResult();

            model.Validate(result.Errors);
            ModelValidator.Validate(jobDataMap, result.Errors);

            if (result.Success)
            {
                IJobDetail BuildJob(JobBuilder builder)
                {
                    return builder
                        .OfType(Type.GetType(jobModel.Type, true))
                        .WithIdentity(jobModel.JobName, jobModel.Group)
                        .WithDescription(jobModel.Description)
                        .SetJobData(jobDataMap.GetQuartzJobDataMap())
                        .RequestRecovery(jobModel.Recovery)
                        .Build();
                }

                if (jobModel.IsNew)
                {
                    await Scheduler.AddJob(BuildJob(JobBuilder.Create().StoreDurably()), replace: false);
                }
                else
                {
                    var oldJob = await GetJobDetail(JobKey.Create(jobModel.OldJobName, jobModel.OldGroup));
                    await Scheduler.UpdateJob(oldJob.Key, BuildJob(oldJob.GetJobBuilder()));
                }

                if (trigger)
                {
                    await Scheduler.TriggerJob(JobKey.Create(jobModel.JobName, jobModel.Group));
                }
            }

            return Json(result);
        }

        [HttpPost, JsonErrorResponse]
        [AuthorizeUser(UserPermissions.DeleteJobs)]
        public async Task<IActionResult> Delete([FromBody] KeyModel model)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var key = model.ToJobKey();

            if (!await Scheduler.DeleteJob(key))
                throw new InvalidOperationException("Cannot delete job " + key);

            return NoContent();
        }

        [HttpGet, JsonErrorResponse]
        [AuthorizeUser(UserPermissions.ViewJobs)]
        public async Task<IActionResult> AdditionalData()
        {
            var keys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var history = await Scheduler.Context.GetExecutionHistoryStore().FilterLastOfEveryJob(10);
            var historyByJob = history.ToLookup(x => x.Job);

            var list = new List<object>();
            foreach (var key in keys)
            {
                var triggers = await Scheduler.GetTriggersOfJob(key);

                var nextFires = triggers.Select(x => x.GetNextFireTimeUtc()?.UtcDateTime).ToArray();

                list.Add(new
                {
                    JobName = key.Name, key.Group,
                    History = historyByJob.TryGet(key.ToString()).ToHistogram(),
                    NextFireTime = nextFires.Where(x => x != null).OrderBy(x => x).FirstOrDefault()?.ToDefaultFormat(),
                });
            }

            return View(list);
        }

        [HttpGet]
        [AuthorizeUser(UserPermissions.CreateNewJobs)]
        public Task<IActionResult> Duplicate(string name, string group)
        {
            return Edit(name, group, clone: true);
        }

        bool EnsureValidKey(string name, string group) => !(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(group));
        bool EnsureValidKey(KeyModel model) => EnsureValidKey(model.Name, model.Group);

    }
}
