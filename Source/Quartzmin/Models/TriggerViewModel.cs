using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartzmin.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using static Quartz.MisfireInstruction;

namespace Quartzmin.Models
{
    public class TriggerViewModel : IHasValidation
    {
        public TriggerPropertiesViewModel Trigger { get; set; }
        public JobDataMapModel DataMap { get; set; }

        public void Validate(ICollection<ValidationError> errors) => ModelValidator.ValidateObject(this, errors);
    }

    public class CronTriggerViewModel : IHasValidation
    {
        [Required]
        public string Expression { get; set; }
        public string TimeZone { get; set; }

        public void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(TriggerViewModel.Trigger), nameof(TriggerPropertiesViewModel.Cron));
        }

        public static CronTriggerViewModel FromTrigger(ICronTrigger trigger)
        {
            return new CronTriggerViewModel()
            {
                Expression = trigger.CronExpressionString,
                TimeZone = trigger.TimeZone.Id,
            };
        }

        public void Apply(TriggerBuilder builder, TriggerPropertiesViewModel model)
        {
            builder.WithCronSchedule(Expression, x =>
            {
                 if (!string.IsNullOrEmpty(TimeZone))
                     x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

                 switch (model.MisfireInstruction)
                 {
                     case InstructionNotSet:
                         break;
                     case IgnoreMisfirePolicy:
                         x.WithMisfireHandlingInstructionIgnoreMisfires();
                         break;
                     case CronTrigger.DoNothing:
                         x.WithMisfireHandlingInstructionDoNothing();
                         break;
                     case CronTrigger.FireOnceNow:
                         x.WithMisfireHandlingInstructionFireAndProceed();
                         break;
                     default:
                         throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                 }
            });
        }
    }

    public class SimpleTriggerViewModel : IHasValidation
    {
        [Required]
        public int? RepeatInterval { get; set; }

        [Required]
        public IntervalUnit RepeatUnit { get; set; }

        public int? RepeatCount { get; set; }

        public bool RepeatForever { get; set; }

        public void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(TriggerViewModel.Trigger), nameof(TriggerPropertiesViewModel.Simple));

            if (RepeatForever == false && RepeatCount == null)
                errors.Add(ValidationError.EmptyField("trigger[simple.repeatCount]"));
        }

        public static SimpleTriggerViewModel FromTrigger(ISimpleTrigger trigger)
        {
            var model = new SimpleTriggerViewModel()
            {
                RepeatCount = trigger.RepeatCount,
                RepeatForever = trigger.RepeatCount == SimpleTriggerImpl.RepeatIndefinitely,
                RepeatInterval = (int)trigger.RepeatInterval.TotalMilliseconds,
                RepeatUnit = IntervalUnit.Millisecond,
            };

            if (model.RepeatCount == -1)
                model.RepeatCount = null;

            if (trigger.RepeatInterval.Milliseconds == 0 && model.RepeatInterval > 0)
            {
                model.RepeatInterval = (int)trigger.RepeatInterval.TotalSeconds;
                model.RepeatUnit = IntervalUnit.Second;
                if (trigger.RepeatInterval.Seconds == 0)
                {
                    model.RepeatInterval = (int)trigger.RepeatInterval.TotalMinutes;
                    model.RepeatUnit = IntervalUnit.Minute;
                    if (trigger.RepeatInterval.Minutes == 0)
                    {
                        model.RepeatInterval = (int)trigger.RepeatInterval.TotalHours;
                        model.RepeatUnit = IntervalUnit.Hour;
                        if (trigger.RepeatInterval.Hours == 0)
                        {
                            model.RepeatInterval = (int)trigger.RepeatInterval.TotalDays;
                            model.RepeatUnit = IntervalUnit.Day;
                        }
                    }
                }
            }

            return model;
        }

        TimeSpan GetRepeatIntervalTimeSpan()
        {
            switch (RepeatUnit)
            {
                case IntervalUnit.Millisecond:
                    return TimeSpan.FromMilliseconds(RepeatInterval.Value);
                case IntervalUnit.Second:
                    return TimeSpan.FromSeconds(RepeatInterval.Value);
                case IntervalUnit.Minute:
                    return TimeSpan.FromMinutes(RepeatInterval.Value);
                case IntervalUnit.Hour:
                    return TimeSpan.FromHours(RepeatInterval.Value);
                case IntervalUnit.Day:
                    return TimeSpan.FromDays(RepeatInterval.Value);
                default:
                    throw new ArgumentException("Invalid value: " + RepeatUnit, nameof(RepeatUnit));
            }
        }

        public void Apply(TriggerBuilder builder, TriggerPropertiesViewModel model)
        {
            builder.WithSimpleSchedule(x =>
            {
                x.WithInterval(GetRepeatIntervalTimeSpan());

                if (RepeatForever)
                    x.RepeatForever();
                else
                    x.WithRepeatCount(RepeatCount.Value);

                switch (model.MisfireInstruction)
                {
                    case InstructionNotSet:
                        break;
                    case IgnoreMisfirePolicy:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case SimpleTrigger.FireNow:
                        x.WithMisfireHandlingInstructionFireNow();
                        break;
                    case SimpleTrigger.RescheduleNowWithExistingRepeatCount:
                        x.WithMisfireHandlingInstructionNowWithExistingCount();
                        break;
                    case SimpleTrigger.RescheduleNowWithRemainingRepeatCount:
                        x.WithMisfireHandlingInstructionNowWithRemainingCount();
                        break;
                    case SimpleTrigger.RescheduleNextWithRemainingCount:
                        x.WithMisfireHandlingInstructionNextWithExistingCount();
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                }
            });
        }

    }

    public class DailyTriggerViewModel : IHasValidation
    {
        public DaysOfWeekViewModel DaysOfWeek { get; set; } = new DaysOfWeekViewModel();

        [Required]
        public int? RepeatInterval { get; set; }

        [Required]
        public IntervalUnit RepeatUnit { get; set; }

        public int? RepeatCount { get; set; }

        public bool RepeatForever { get; set; }

        [Required]
        public TimeSpan? StartTime { get; set; }

        [Required]
        public TimeSpan? EndTime { get; set; }

        public string TimeZone { get; set; }

        public void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(TriggerViewModel.Trigger), nameof(TriggerPropertiesViewModel.Daily));

            if (RepeatForever == false && RepeatCount == null)
                errors.Add(ValidationError.EmptyField("trigger[daily.repeatCount]"));
        }

        public static DailyTriggerViewModel FromTrigger(IDailyTimeIntervalTrigger trigger)
        {
            var model = new DailyTriggerViewModel()
            {
                RepeatCount = trigger.RepeatCount,
                RepeatInterval = trigger.RepeatInterval,
                RepeatUnit = trigger.RepeatIntervalUnit,
                StartTime = trigger.StartTimeOfDay.ToTimeSpan(),
                EndTime = trigger.EndTimeOfDay.ToTimeSpan(),
                DaysOfWeek = DaysOfWeekViewModel.Create(trigger.DaysOfWeek),
                TimeZone = trigger.TimeZone.Id,
            };

            if (model.RepeatCount == -1)
            {
                model.RepeatCount = null;
                model.RepeatForever = true;
            }

            return model;
        }

        public void Apply(TriggerBuilder builder, TriggerPropertiesViewModel model)
        {
            builder.WithDailyTimeIntervalSchedule(x =>
            {
                if (!string.IsNullOrEmpty(TimeZone))
                    x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

                if (!RepeatForever)
                    x.WithRepeatCount(RepeatCount.Value);

                x.WithInterval(RepeatInterval.Value, RepeatUnit);
                x.StartingDailyAt(StartTime.Value.ToTimeOfDay());
                x.EndingDailyAt(EndTime.Value.ToTimeOfDay());
                x.OnDaysOfTheWeek(DaysOfWeek.GetSelected().ToArray());

                switch (model.MisfireInstruction)
                {
                    case InstructionNotSet:
                        break;
                    case IgnoreMisfirePolicy:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case DailyTimeIntervalTrigger.DoNothing:
                        x.WithMisfireHandlingInstructionDoNothing();
                        break;
                    case DailyTimeIntervalTrigger.FireOnceNow:
                        x.WithMisfireHandlingInstructionFireAndProceed();
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                }
            });
        }

    }


    public class CalendarTriggerViewModel : IHasValidation
    {
        [Required]
        public int? RepeatInterval { get; set; } // nullable to validate missing value

        [Required]
        public IntervalUnit RepeatUnit { get; set; }

        public string TimeZone { get; set; }

        public bool PreserveHourAcrossDst { get; set; }

        public bool SkipDayIfHourDoesNotExist { get; set; }

        public void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(TriggerViewModel.Trigger), nameof(TriggerPropertiesViewModel.Calendar));
        }

        public static CalendarTriggerViewModel FromTrigger(ICalendarIntervalTrigger trigger)
        {
            return new CalendarTriggerViewModel()
            {
                RepeatInterval = trigger.RepeatInterval,
                RepeatUnit = trigger.RepeatIntervalUnit,
                PreserveHourAcrossDst = trigger.PreserveHourOfDayAcrossDaylightSavings,
                SkipDayIfHourDoesNotExist = trigger.SkipDayIfHourDoesNotExist,
                TimeZone = trigger.TimeZone.Id,
            };
        }

        public void Apply(TriggerBuilder builder, TriggerPropertiesViewModel model)
        {
            builder.WithCalendarIntervalSchedule(x =>
            {
                if (!string.IsNullOrEmpty(TimeZone))
                    x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

                x.WithInterval(RepeatInterval.Value, RepeatUnit);
                x.PreserveHourOfDayAcrossDaylightSavings(PreserveHourAcrossDst);
                x.SkipDayIfHourDoesNotExist(SkipDayIfHourDoesNotExist);

                switch (model.MisfireInstruction)
                {
                    case InstructionNotSet:
                        break;
                    case IgnoreMisfirePolicy:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case CalendarIntervalTrigger.DoNothing:
                        x.WithMisfireHandlingInstructionDoNothing();
                        break;
                    case CalendarIntervalTrigger.FireOnceNow:
                        x.WithMisfireHandlingInstructionFireAndProceed();
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                }
            });
        }

    }

    public class DaysOfWeekViewModel
    {
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        public void AllOn()
        {
            Monday = true;
            Tuesday = true;
            Wednesday = true;
            Thursday = true;
            Friday = true;
            Saturday = true;
            Sunday = true;
        }

        public static DaysOfWeekViewModel Create(IEnumerable<DayOfWeek> list)
        {
            var model = new DaysOfWeekViewModel();
            foreach (var item in list)
            {
                if (item == DayOfWeek.Sunday)
                    model.Sunday = true;
                if (item == DayOfWeek.Monday)
                    model.Monday = true;
                if (item == DayOfWeek.Tuesday)
                    model.Tuesday = true;
                if (item == DayOfWeek.Wednesday)
                    model.Wednesday = true;
                if (item == DayOfWeek.Thursday)
                    model.Thursday = true;
                if (item == DayOfWeek.Friday)
                    model.Friday = true;
                if (item == DayOfWeek.Saturday)
                    model.Saturday = true;
            }
            return model;
        }

        public IEnumerable<DayOfWeek> GetSelected()
        {
            if (Monday) yield return DayOfWeek.Monday;
            if (Tuesday) yield return DayOfWeek.Tuesday;
            if (Wednesday) yield return DayOfWeek.Wednesday;
            if (Thursday) yield return DayOfWeek.Thursday;
            if (Friday) yield return DayOfWeek.Friday;
            if (Saturday) yield return DayOfWeek.Saturday;
            if (Sunday) yield return DayOfWeek.Sunday;
        }

        public bool AreOnlyWeekendEnabled => !Monday && !Tuesday && !Wednesday && !Thursday && !Friday && Saturday && Sunday;
        public bool AreOnlyWeekdaysEnabled => Monday && Tuesday && Wednesday && Thursday && Friday && !Saturday && !Sunday;
    }

    public enum TriggerType
    {
        Unknown = 0,
        Cron,
        Simple,
        Daily,
        Calendar,
    }

    public class TriggerPropertiesViewModel : IHasValidation
    {
        public string DateFormat { get; } = DateTimeSettings.DefaultDateFormat;
        public string TimeFormat { get; } = DateTimeSettings.DefaultTimeFormat;
        public string DateTimeFormat { get => DateFormat + " " + TimeFormat; }

        [SkipValidation] public SimpleTriggerViewModel Simple { get; set; } = new SimpleTriggerViewModel();
        [SkipValidation] public DailyTriggerViewModel Daily { get; set; } = new DailyTriggerViewModel();
        [SkipValidation] public CronTriggerViewModel Cron { get; set; } = new CronTriggerViewModel();
        [SkipValidation] public CalendarTriggerViewModel Calendar { get; set; } = new CalendarTriggerViewModel();

        public bool IsNew { get; set; }

        public bool IsCopy { get; set; }

        public TriggerType Type { get; set; }

        [Required]
        public string Job { get; set; }

        public IEnumerable<string> JobList { get; set; }

        [Required]
        public string TriggerName { get; set; }

        [Required]
        public string TriggerGroup { get; set; }

        public string OldTriggerName { get; set; }

        public string OldTriggerGroup { get; set; }

        public IEnumerable<string> TriggerGroupList { get; set; }

        public string Description { get; set; }

        public string StartTimeUtc { get; set; }
        public string EndTimeUtc { get; set; }

        public DateTime? GetStartTimeUtc() => ParseDateTime(StartTimeUtc);

        public DateTime? GetEndTimeUtc() => ParseDateTime(EndTimeUtc);

        public Dictionary<string, string> TimeZoneList { get => TimeZoneInfo.GetSystemTimeZones().ToDictionary(); }

        DateTime? ParseDateTime(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (DateTime.TryParseExact(value, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result) == false)
                return null;

            return result;
        }

        public string CalendarName { get; set; }

        public IEnumerable<string> CalendarNameList { get; set; }

        [Required]
        public int? Priority { get; set; }

        public IEnumerable<string> PriorityList => Enumerable.Range(1, 10).Select(x => x.ToString());

        public int PriorityOrDefault => Priority ?? 5;

        [Required]
        public int? MisfireInstruction { get; set; }

        public void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(TriggerViewModel.Trigger));

            if (Type == TriggerType.Unknown)
                errors.Add(ValidationError.EmptyField("trigger[type]"));

            if (Type == TriggerType.Daily)
                Daily.Validate(errors);
            if (Type == TriggerType.Calendar)
                Calendar.Validate(errors);
            if (Type == TriggerType.Cron)
                Cron.Validate(errors);
            if (Type == TriggerType.Simple)
                Simple.Validate(errors);
        }

        public string MisfireInstructionsJson => _misfireInstructionsJson;

        static readonly string _misfireInstructionsJson = CreateMisfireInstructionsJson();

        private static string CreateMisfireInstructionsJson()
        {
            var standardMisfireInstructions = new Dictionary<int, string>()
            {
                [0] = "Smart Policy",
                [1] = "Fire Once Now",
                [2] = "Do Nothing",
            };

            var validMisfireInstructions = new Dictionary<string, Dictionary<int, string>>()
            {
                ["cron"] = standardMisfireInstructions,
                ["calendar"] = standardMisfireInstructions,
                ["daily"] = standardMisfireInstructions,
                ["simple"] = new Dictionary<int, string>()
                {
                    [0] = "Smart Policy",
                    [1] = "Fire Now",
                    [2] = "Reschedule Now With Existing Repeat Count",
                    [3] = "Reschedule Now With Remaining Repeat Count",
                    [4] = "Reschedule Next With Remaining Count",
                    [5] = "Reschedule Next With Existing Count",
                },
            };

            return JsonConvert.SerializeObject(validMisfireInstructions, Formatting.None);
        }

        public static async Task<TriggerPropertiesViewModel> Create(IScheduler scheduler)
        {
            var model = new TriggerPropertiesViewModel()
            {
                TriggerGroupList = (await scheduler.GetTriggerGroupNames()).GroupArray(),
                TriggerGroup = SchedulerConstants.DefaultGroup,
                JobList = (await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup())).Select(x => x.ToString()).ToArray(),
                CalendarNameList = await scheduler.GetCalendarNames(),
            };

            model.Cron.TimeZone = TimeZoneInfo.Local.Id;

            model.Simple.RepeatInterval = 1;
            model.Simple.RepeatUnit = IntervalUnit.Minute;
            model.Simple.RepeatForever = true;

            model.Daily.DaysOfWeek.AllOn();
            model.Daily.RepeatInterval = 1;
            model.Daily.RepeatUnit = IntervalUnit.Minute;
            model.Daily.RepeatForever = true;
            model.Daily.TimeZone = TimeZoneInfo.Local.Id;

            model.Calendar.RepeatInterval = 1;
            model.Calendar.RepeatUnit = IntervalUnit.Minute;
            model.Calendar.TimeZone = TimeZoneInfo.Local.Id;

            return model;
        }

    }
}