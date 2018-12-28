using Quartz;
using Quartzmin.Models;
using Quartzmin.TypeHandlers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory;

#region Target-Specific Directives
#if NETSTANDARD
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
#endif
#if NETFRAMEWORK
using HttpRequest = System.Net.Http.HttpRequestMessage;
#endif
#endregion

namespace Quartzmin
{
    internal static class Extensions
    {
        public static TypeHandlerBase[] Order(this IEnumerable<TypeHandlerBase> typeHandlers)
        {
            return typeHandlers.OrderBy(x => x.DisplayName).ToArray();
        }

        public static JobDataMapItemBase[] GetModel(this IEnumerable<Dictionary<string, object>> formData, Services services)
        {
            return formData.Select(x => JobDataMapItemBase.FromDictionary(x, services)).Where(x => !x.IsLast).ToArray();
        }

        public static string ToDefaultFormat(this DateTime date)
        {
            return date.ToString(DateTimeSettings.DefaultDateFormat + " " + DateTimeSettings.DefaultTimeFormat, CultureInfo.InvariantCulture);
        }

        public static Dictionary<string, string> ToDictionary(this IEnumerable<TimeZoneInfo> timeZoneInfos)
        {
            return timeZoneInfos.ToDictionary(x => x.Id, x =>
            {
                var title = x.ToString();
                if (!title.StartsWith("("))
                    title = $"({title}) {x.Id}";
                return title;
            });
        }

        public static IEnumerable<ICalendar> Flatten(this ICalendar root)
        {
            while (root != null)
            {
                yield return root;
                root = root.CalendarBase;
            }
        }

        public static string ETag(this DateTime dateTime)
        {
            long etagHash = dateTime.ToFileTimeUtc();
            return '\"' + Convert.ToString(etagHash, 16) + '\"';
        }

        public static string ReadAsString(this HttpRequest request)
        {
#if NETSTANDARD
            using (var ms = new MemoryStream())
            {
                request.Body.CopyTo(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
#endif
#if NETFRAMEWORK
            return request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
#endif
        }

        public static JobDataMap GetQuartzJobDataMap(this IEnumerable<JobDataMapItemBase> models)
        {
            var map = new JobDataMap();

            foreach (var item in models)
                map.Put(item.Name, item.Value);

            return map;
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using (var it = source.GetEnumerator())
            {
                if (it.MoveNext())
                {
                    var item = it.Current;
                    while (it.MoveNext())
                    {
                        yield return item;
                        item = it.Current;
                    }
                }
            }
        }

        public static object GetValue(this IDictionary<string, object> dict, string key, object @default)
        {
            if (dict.TryGetValue(key, out var value))
                return value;
            else
                return @default;
        }

        public static string[] GroupArray(this IEnumerable<string> seq)
        {
            return seq.Concat(new[] { SchedulerConstants.DefaultGroup }).Distinct().OrderBy(x => x).ToArray();
        }

        public static string[] GroupArray(this IEnumerable<JobKey> seq)
        {
            return seq.Select(x => x.Group).GroupArray();
        }

        public static string RemoveAssemblyDetails(this Type type)
        {
            // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Utilities/ReflectionUtils.cs

            string fullyQualifiedTypeName = type.AssemblyQualifiedName;

            StringBuilder builder = new StringBuilder();

            // loop through the type name and filter out qualified assembly details from nested type names
            bool writingAssemblyName = false;
            bool skippingAssemblyDetails = false;
            for (int i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                char current = fullyQualifiedTypeName[i];
                switch (current)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(current);
                        }
                        else
                        {
                            skippingAssemblyDetails = true;
                        }
                        break;
                    default:
                        if (!skippingAssemblyDetails)
                        {
                            builder.Append(current);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        public static List<JobDataMapItem> GetJobDataMapModel(this IJobDetail job, Services services) => GetJobDataMapModelCore(job, services);
        public static List<JobDataMapItem> GetJobDataMapModel(this ITrigger trigger, Services services) => GetJobDataMapModelCore(trigger, services);

        private static List<JobDataMapItem> GetJobDataMapModelCore(object jobOrTrigger, Services services)
        {
            List<JobDataMapItem> list = new List<JobDataMapItem>();

            // TODO: doplnit parametre z template na zaklade jobKey; value najprv skonvertovat na ocakavany typ zo sablony

            JobDataMap jobDataMap = null;

            {
                if (jobOrTrigger is IJobDetail j)
                    jobDataMap = j.JobDataMap;
                if (jobOrTrigger is ITrigger t)
                    jobDataMap = t.JobDataMap;
            }

            if (jobDataMap == null)
                throw new ArgumentException("Invalid type.", nameof(jobOrTrigger));

            foreach (var pair in jobDataMap)
            {
                JobDataMapItem model;

                model = new JobDataMapItem()
                {
                    Enabled = true,
                    Name = pair.Key,
                    Value = pair.Value,
                };

                var typeHandlers = new List<TypeHandlerBase>();
                typeHandlers.AddRange(services.Options.StandardTypes);

                if (model.Value == null)
                {
                    model.SelectedType = services.Options.DefaultSelectedType;
                }
                else
                {
                    // find the best TypeHandler
                    foreach (var t in typeHandlers)
                    {
                        if (t.CanHandle(model.Value))
                        {
                            model.SelectedType = t;
                            break;
                        }
                    }

                    if (model.SelectedType == null) // if there is no suitable TypeHandler, create dynamic one
                    {
                        Type t = model.Value.GetType();

                        string strValue;
                        var m = t.GetMethod(nameof(ToString), BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, new Type[0], null);
                        if (m.DeclaringType == typeof(object))
                            strValue = "{" + t.ToString() + "}";
                        else
                            strValue = string.Format(CultureInfo.InvariantCulture, "{0}", model.Value);

                        model.SelectedType = new UnsupportedTypeHandler()
                        {
                            Name = Guid.NewGuid().ToString("N"), // assure unique name
                            AssemblyQualifiedName = t.RemoveAssemblyDetails(),
                            DisplayName = "Unsupported",
                            StringValue = strValue
                        };

                        typeHandlers.Add(model.SelectedType);
                    }
                }

                model.SupportedTypes = typeHandlers.Order();

                list.Add(model);
            }

            return list;
        }

        public static Task UpdateJob(this IScheduler scheduler, JobKey jobKey, IJobDetail newJob)
        {
            return Task.Run(async () =>
            {
                // get existing triggers associated with job
                var triggers = await scheduler.GetTriggersOfJob(jobKey);

                // set new job to all triggers
                triggers = triggers.Select(t =>
                {
                    var b = t.GetTriggerBuilder().ForJob(newJob.Key);
                    if (t.StartTimeUtc < DateTimeOffset.UtcNow) b.StartNow();
                    return b.Build();
                }).ToArray();
                
                // delete old job
                await scheduler.DeleteJob(jobKey);

                // save new job with triggers
                await scheduler.ScheduleJob(newJob, triggers, replace: true);
            });
        }

        public static TriggerType GetTriggerType(this ITrigger trigger)
        {
            if (trigger is ICronTrigger)
                return TriggerType.Cron;
            if (trigger is IDailyTimeIntervalTrigger)
                return TriggerType.Daily;
            if (trigger is ISimpleTrigger)
                return TriggerType.Simple;
            if (trigger is ICalendarIntervalTrigger)
                return TriggerType.Calendar;

            return TriggerType.Unknown;
        }

        public static string GetScheduleDescription(this ITrigger trigger)
        {
            if (trigger is ICronTrigger cr)
                return CronExpressionDescriptor.ExpressionDescriptor.GetDescription(cr.CronExpressionString);
            if (trigger is IDailyTimeIntervalTrigger dt)
                return GetScheduleDescription(dt);
            if (trigger is ISimpleTrigger st)
                return GetScheduleDescription(st);
            if (trigger is ICalendarIntervalTrigger ct)
                return GetScheduleDescription(ct.RepeatInterval, ct.RepeatIntervalUnit);

            return null;
        }

        private class TimespanPart
        {
            public static readonly TimespanPart[] Items = new[]
            {
            new TimespanPart("day", 1000 * 60 * 60 * 24),
            new TimespanPart("hour", 1000 * 60 * 60),
            new TimespanPart("minute", 1000 * 60),
            new TimespanPart("second", 1000),
            new TimespanPart("millisecond", 1),
        };

            public string Singular { get; set; }
            public string Plural { get; set; }
            public long Multiplier { get; set; }

            public TimespanPart(string singular, long multiplier) : this(singular)
            {
                Multiplier = multiplier;
            }
            public TimespanPart(string singular)
            {
                Singular = singular;
                Plural = singular + "s";
            }
        }

        public static string GetScheduleDescription(this IDailyTimeIntervalTrigger trigger)
        {
            string result = GetScheduleDescription(trigger.RepeatInterval, trigger.RepeatIntervalUnit, trigger.RepeatCount);
            result += " from " + trigger.StartTimeOfDay.ToShortFormat() + " to " + trigger.EndTimeOfDay.ToShortFormat();

            if (trigger.DaysOfWeek.Count < 7)
            {
                var dow = DaysOfWeekViewModel.Create(trigger.DaysOfWeek);

                if (dow.AreOnlyWeekdaysEnabled)
                    result += " only on Weekdays";
                else if (dow.AreOnlyWeekendEnabled)
                    result += " only on Weekends";
                else
                    result += " on " + string.Join(", ", trigger.DaysOfWeek);
            }

            return result;
        }

        public static string GetScheduleDescription(this ISimpleTrigger trigger)
        {
            string result = "Repeat ";
            if (trigger.RepeatCount > 0)
                result += trigger.RepeatCount + " times ";
            result += "every ";

            var diff = trigger.RepeatInterval.TotalMilliseconds;

            var messagesParts = new List<string>();
            foreach (var part in TimespanPart.Items)
            {
                var currentPartValue = Math.Floor(diff / part.Multiplier);
                diff -= currentPartValue * part.Multiplier;

                if (currentPartValue == 1)
                    messagesParts.Add(part.Singular);
                else if (currentPartValue > 1)
                    messagesParts.Add(currentPartValue + " " + part.Plural);
            }

            result += string.Join(", ", messagesParts);

            return result;
        }

        public static string GetScheduleDescription(int repeatInterval, IntervalUnit repeatIntervalUnit, int repeatCount = 0)
        {
            string result = "Repeat ";
            if (repeatCount > 0)
                result += repeatCount + " times ";
            result += "every ";

            string unitStr = repeatIntervalUnit.ToString().ToLower();

            if (repeatInterval == 1)
                result += unitStr;
            else
                result += repeatInterval + " " + unitStr + "s";

            return result;
        }
        

        public static string ToShortFormat(this TimeOfDay timeOfDay)
        {
            return timeOfDay.ToTimeSpan().ToString("g", CultureInfo.InvariantCulture);
        }

        public static TimeSpan ToTimeSpan(this TimeOfDay timeOfDay)
        {
            return TimeSpan.FromSeconds(timeOfDay.Second + timeOfDay.Minute * 60 + timeOfDay.Hour * 3600);
        }

        public static TriggerBuilder ForJob(this TriggerBuilder builder, string jobKey)
        {
            var parts = jobKey.Split('.');
            return builder.ForJob(new JobKey(parts[1], parts[0]));
        }

        public static TimeOfDay ToTimeOfDay(this TimeSpan timeSpan)
        {
            return new TimeOfDay(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        public static IEnumerable<TElement> TryGet<TKey, TElement>(this ILookup<TKey, TElement> lookup, TKey key)
        {
            return lookup.Contains(key) ? lookup[key] : null;
        }

        public static Histogram ToHistogram(this IEnumerable<ExecutionHistoryEntry> entries, bool detailed = false)
        {
            if (entries == null || entries.Any() == false)
                return null;

            var hst = new Histogram();
            foreach (var entry in entries)
            {
                TimeSpan? duration = null;
                string cssClass = "";
                string state = "Finished";

                if (entry.FinishedTimeUtc != null)
                    duration = entry.FinishedTimeUtc - entry.ActualFireTimeUtc;

                if (entry.Vetoed == false && entry.FinishedTimeUtc == null) // still running
                {
                    duration = DateTime.UtcNow - entry.ActualFireTimeUtc;
                    cssClass = "running";
                    state = "Running";
                }

                if (entry.Vetoed)
                    state = "Vetoed";

                string durationHtml = "", delayHtml = "", errorHtml = "", detailsHtml = "";

                if (!string.IsNullOrEmpty(entry.ExceptionMessage))
                {
                    state = "Failed";
                    cssClass = "failed";
                    errorHtml = $"<br>Error: <b>{entry.ExceptionMessage}</b>";
                }

                if (duration != null)
                    durationHtml = $"<br>Duration: <b>{duration.ToNiceFormat()}</b>";

                if (entry.ScheduledFireTimeUtc != null)
                    delayHtml = $"<br>Delay: <b>{(entry.ActualFireTimeUtc - entry.ScheduledFireTimeUtc).ToNiceFormat()}</b>";

                if (detailed)
                    detailsHtml = $"Job: <b>{entry.Job}</b><br>Trigger: <b>{entry.Trigger}</b><br>";

                hst.AddBar(duration?.TotalSeconds ?? 1, 
                    $"{detailsHtml}Fired: <b>{entry.ActualFireTimeUtc.ToDefaultFormat()} UTC</b>{durationHtml}{delayHtml}"+
                    $"<br>State: <b>{state}</b>{errorHtml}",
                    cssClass);
            }

            return hst;
        }

        public static string ToNiceFormat(this TimeSpan? timeSpan)
        {
            if (timeSpan == null) return "";

            var ts = timeSpan.Value;

            if (ts.TotalSeconds < 1)
                return (int)ts.TotalMilliseconds + "ms";

            if (ts.TotalMinutes < 1)
                return (int)ts.TotalSeconds + " seconds";

            if (ts.TotalHours < 1)
                return (int)ts.TotalMinutes + " minutes";

            if (ts.TotalDays < 1)
                return string.Format(CultureInfo.InvariantCulture, "{0:hh\\:mm}", timeSpan);

            return string.Format(CultureInfo.InvariantCulture, "{0:%d} days {0:hh\\:mm}", timeSpan);
        }

    }
}
