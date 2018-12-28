using Quartz;
using Quartz.Impl.Calendar;
using Quartzmin.Helpers;
using Quartzmin.TypeHandlers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Quartzmin.Models
{
    public class CalendarViewModel : IHasValidation
    {
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        public bool IsRoot { get; set; }

        public string CustomType { get; set; }

        public string Description { get; set; }

        public string TimeZone { get; set; }

        public string CronExpression { get; set; }

        public bool InvertTimeRange { get; set; }

        public string StartingTime { get; set; }

        public string EndingTime { get; set; }

        public List<string> Days { get; set; }

        public List<string> Dates { get; set; }

        public bool[] DaysExcluded { get; set; }

        public void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, camelCase: false);

            if (knownCalendars.TryGetValue(Type, out var o))
                o.Validator(this, errors);
            else
                errors.Add(new ValidationError() { Field = nameof(Type), Reason = "Invalid value." });
        }

        public static CalendarViewModel FromCalendar(ICalendar calendar)
        {
            if (converters.TryGetValue(calendar.GetType(), out var modelFactory))
                return modelFactory(calendar);

            if (calendar is BaseCalendar)
                return converters[typeof(BaseCalendar)](calendar);

            return converters[typeof(ICalendar)](calendar);
        }

        private class CalendarHandler
        {
            public Action<CalendarViewModel, ICollection<ValidationError>> Validator { get; set; }
            public Func<CalendarViewModel, ICalendar> Builder { get; set; }
        }

        private TimeZoneInfo ResolveTimeZone()
        {
            if (string.IsNullOrEmpty(TimeZone))
                return null;
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
        }

        public ICalendar BuildCalendar()
        {
            if (knownCalendars.TryGetValue(Type, out var o))
                return o.Builder(this);
            throw new InvalidOperationException("Unsupported Type: " + Type);
        }

        private static readonly Dictionary<string, CalendarHandler> knownCalendars = new Dictionary<string, CalendarHandler>()
        {
            ["annual"] = new CalendarHandler()
            {
                Builder = model =>
                {
                    var cal = new AnnualCalendar() { TimeZone = model.ResolveTimeZone(), Description = model.Description };
                    foreach (var d in model.Days)
                        cal.SetDayExcluded(DateTime.ParseExact(d, "MMMM d", CultureInfo.InvariantCulture), true);
                    return cal;
                },
                Validator = (model, errors) =>
                {
                    for (int i = 0; i < model.Days.Count; i++)
                    {
                        if (DateTime.TryParseExact(model.Days[i], "MMMM d", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _) == false)
                            errors.Add(new ValidationError() { Field = nameof(model.Days), Reason = "Invalid format.", FieldIndex = i });
                    }
                }
            },
            ["cron"] = new CalendarHandler()
            {
                Builder = model =>
                {
                    return new CronCalendar(model.CronExpression) { TimeZone = model.ResolveTimeZone(), Description = model.Description };
                },
                Validator = (model, errors) =>
                {
                    if (string.IsNullOrEmpty(model.CronExpression))
                        errors.Add(ValidationError.EmptyField(nameof(model.CronExpression)));
                    else
                    {
                        if (Quartz.CronExpression.IsValidExpression(model.CronExpression) == false)
                            errors.Add(new ValidationError() { Field = nameof(model.CronExpression), Reason = "Invalid format." });
                    }

                }
            },
            ["daily"] = new CalendarHandler()
            {
                Builder = model =>
                {
                    return new DailyCalendar(model.StartingTime, model.EndingTime)
                    {
                        TimeZone = model.ResolveTimeZone(),
                        InvertTimeRange = model.InvertTimeRange,
                        Description = model.Description
                    };
                },
                Validator = (model, errors) =>
                {
                    if (string.IsNullOrEmpty(model.StartingTime))
                        errors.Add(ValidationError.EmptyField(nameof(model.StartingTime)));
                    else
                    {
                        if (TimeSpan.TryParse(model.StartingTime, out var _) == false)
                            errors.Add(new ValidationError() { Field = nameof(model.StartingTime), Reason = "Invalid format." });
                    }

                    if (string.IsNullOrEmpty(model.EndingTime))
                        errors.Add(ValidationError.EmptyField(nameof(model.EndingTime)));
                    else
                    {
                        if (TimeSpan.TryParse(model.EndingTime, out var _) == false)
                            errors.Add(new ValidationError() { Field = nameof(model.EndingTime), Reason = "Invalid format." });
                    }
                }
            },
            ["holiday"] = new CalendarHandler()
            {
                Builder = model =>
                {
                    var cal = new HolidayCalendar() { TimeZone = model.ResolveTimeZone(), Description = model.Description };
                    foreach (var d in model.Dates)
                        cal.AddExcludedDate(DateTime.ParseExact(d, DateTimeSettings.DefaultDateFormat, CultureInfo.InvariantCulture));
                    return cal;
                },
                Validator = (model, errors) =>
                {
                    for (int i = 0; i < model.Dates.Count; i++)
                    {
                        if (DateTime.TryParseExact(model.Dates[i], DateTimeSettings.DefaultDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _) == false)
                            errors.Add(new ValidationError() { Field = nameof(model.Dates), Reason = "Invalid format.", FieldIndex = i });
                    }
                }
            },
            ["monthly"] = new CalendarHandler()
            {
                Builder = model =>
                {
                    var cal = new MonthlyCalendar() { TimeZone = model.ResolveTimeZone(), Description = model.Description };
                    for (int i = 0; i < model.DaysExcluded.Length; i++)
                        cal.SetDayExcluded(i + 1, model.DaysExcluded[i]);
                    return cal;
                },
                Validator = (model, errors) =>
                {
                    if (model.DaysExcluded.Length != 31)
                        errors.Add(new ValidationError() { Field = nameof(model.DaysExcluded), Reason = "Invalid Length." });
                }
            },
            ["weekly"] = new CalendarHandler()
            {
                Builder = model =>
                {
                    var cal = new WeeklyCalendar() { TimeZone = model.ResolveTimeZone(), Description = model.Description };
                    for (int i = 0; i < model.DaysExcluded.Length; i++)
                        cal.SetDayExcluded((DayOfWeek)i, model.DaysExcluded[i]);
                    return cal;
                },
                Validator = (model, errors) =>
                {
                    if (model.DaysExcluded.Length != 7)
                        errors.Add(new ValidationError() { Field = nameof(model.DaysExcluded), Reason = "Invalid Length." });
                }
            },
            ["none"] = new CalendarHandler()
            {
                Builder = model => null,
                Validator = (model, errors) => { }
            },
            ["custom"] = new CalendarHandler()
            {
                Builder = model => throw new InvalidOperationException(),
                Validator = (model, errors) => { }
            }

        };

        private static readonly Dictionary<Type, Func<ICalendar, CalendarViewModel>> converters = new Dictionary<Type, Func<ICalendar, CalendarViewModel>>()
        {
            [typeof(AnnualCalendar)] = calendar =>
            {
                var cal = (AnnualCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "annual",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                    Days = cal.DaysExcluded.Select(x => x.ToString("MMMM d", CultureInfo.InvariantCulture)).ToList(),
                };
            },
            [typeof(CronCalendar)] = calendar =>
            {
                var cal = (CronCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "cron",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                    CronExpression = cal.CronExpression.CronExpressionString
                };
            },
            [typeof(DailyCalendar)] = calendar =>
            {
                var cal = (DailyCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "daily",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                    StartingTime = cal.RangeStartingTime,
                    EndingTime = cal.RangeEndingTime,
                };
            },
            [typeof(HolidayCalendar)] = calendar =>
            {
                var cal = (HolidayCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "holiday",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                    Dates = cal.ExcludedDates.Select(x => x.ToString(DateTimeSettings.DefaultDateFormat, CultureInfo.InvariantCulture)).ToList()
                };
            },
            [typeof(MonthlyCalendar)] = calendar =>
            {
                var cal = (MonthlyCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "monthly",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                    DaysExcluded = cal.DaysExcluded
                };
            },
            [typeof(WeeklyCalendar)] = calendar =>
            {
                var cal = (WeeklyCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "weekly",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                    DaysExcluded = cal.DaysExcluded
                };
            },
            [typeof(BaseCalendar)] = calendar =>
            {
                var cal = (BaseCalendar)calendar;
                return new CalendarViewModel()
                {
                    Type = "custom",
                    Description = cal.Description,
                    TimeZone = cal.TimeZone.Id,
                };
            },
            [typeof(ICalendar)] = calendar =>
            {
                var cal = calendar;
                return new CalendarViewModel()
                {
                    Type = "custom",
                    Description = cal.Description,
                };
            }
        };
    }
}
