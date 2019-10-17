using Quartz;
using Quartzmin.Helpers;
using Quartzmin.Models;
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
    public class CalendarsController : PageControllerBase
    {
        [HttpGet]
        [AuthorizeUser(UserPermissions.ViewCalendars)]
        public async Task<IActionResult> Index()
        {
            var calendarNames = await Scheduler.GetCalendarNames();

            var list = new List<CalendarListItem>();

            foreach (string name in calendarNames)
            {
                var cal = await Scheduler.GetCalendar(name);
                list.Add(new CalendarListItem() { Name = name, Description = cal.Description, Type = cal.GetType() });
            }
            
            return View(list);
        }

        [HttpGet]
        [AuthorizeUser(UserPermissions.CreateNewCalendars)]
        public IActionResult New()
        {
            ViewBag.IsNew = true;
            return View("Edit", new[] { new CalendarViewModel()
            {
                IsRoot = true,
                Type = "cron",
                TimeZone = TimeZoneInfo.Local.Id,
            }});
        }

        [HttpGet]
        [AuthorizeUser(UserPermissions.ViewCalendars)]
        public async Task<IActionResult> Edit(string name)
        {
            var calendar = await Scheduler.GetCalendar(name);

            var model = calendar.Flatten().Select(x => CalendarViewModel.FromCalendar(x)).ToArray();

            if (model.Any())
            {
                model[0].IsRoot = true;
                model[0].Name = name;
            }

            ViewBag.IsNew = false;

            return View(model);
        }

        private void RemoveLastEmpty(List<string> list)
        {
            if (list?.Count > 0 && string.IsNullOrEmpty(list.Last()))
                list.RemoveAt(list.Count - 1);
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Save([FromBody] CalendarViewModel[] chain, bool isNew)
        {
            if ((isNew && !UserHasPermissions(UserPermissions.CreateNewCalendars)) ||
                !UserHasPermissions(UserPermissions.EditCalendars))
            {
                return Unauthorized();
            }

            var result = new ValidationResult();

            if (chain.Length == 0 || string.IsNullOrEmpty(chain[0].Name))
                result.Errors.Add(ValidationError.EmptyField(nameof(CalendarViewModel.Name)));

            for (int i = 0; i < chain.Length; i++)
            {
                RemoveLastEmpty(chain[i].Days);
                RemoveLastEmpty(chain[i].Dates);

                var errors = new List<ValidationError>();
                chain[i].Validate(errors);
                errors.ForEach(x => x.SegmentIndex = i);
                result.Errors.AddRange(errors);
            }
            
            if (result.Success)
            {
                string name = chain[0].Name;

                ICalendar existing = null;

                if (isNew == false)
                    existing = await Scheduler.GetCalendar(name);

                ICalendar root = null, current = null;
                for (int i = 0; i < chain.Length; i++)
                {
                    ICalendar newCal = chain[i].Type.Equals("custom") ? existing : chain[i].BuildCalendar();

                    if (newCal == null)
                        break;

                    if (i == 0)
                        root = newCal;
                    else
                        current.CalendarBase = newCal;

                    current = newCal;
                    existing = existing?.CalendarBase;
                }
                
                if (root == null)
                {
                    result.Errors.Add(new ValidationError() { Field = nameof(CalendarViewModel.Type), Reason = "Cannot create calendar.", SegmentIndex = 0 });
                }
                else
                {
                    await Scheduler.AddCalendar(name, root, replace: true, updateTriggers: true);
                }
            }

            return Json(result);
        }

        public class DeleteArgs
        {
            public string Name { get; set; }
        }


        [HttpPost, JsonErrorResponse]
        [AuthorizeUser(UserPermissions.DeleteCalendars)]
        public async Task<IActionResult> Delete([FromBody] DeleteArgs args)
        {
            if (!await Scheduler.DeleteCalendar(args.Name))
                throw new InvalidOperationException("Cannot delete calendar " + args.Name);

            return NoContent();
        }

    }
}
