using System.Collections.Generic;

namespace CronExpressionDescriptor
{
    internal static class Resources
    {
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>()
        {
            ["AMPeriod"] = "AM",
            ["AnErrorOccuredWhenGeneratingTheExpressionD"] = "An error occured when generating the expression description.  Check the cron expression syntax.",
            ["At"] = "At",
            ["AtSpace"] = "At ",
            ["AtX0"] = "at {0}",
            ["AtX0MinutesPastTheHour"] = "at {0} minutes past the hour",
            ["AtX0SecondsPastTheMinute"] = "at {0} seconds past the minute",
            ["BetweenX0AndX1"] = "between {0} and {1}",
            ["ComaBetweenDayX0AndX1OfTheMonth"] = ", between day {0} and {1} of the month",
            ["ComaEveryDay"] = ", every day",
            ["ComaEveryHour"] = ", every hour",
            ["ComaEveryMinute"] = ", every minute",
            ["ComaEveryX0Days"] = ", every {0} days",
            ["ComaEveryX0DaysOfTheWeek"] = ", every {0} days of the week",
            ["ComaEveryX0Months"] = ", every {0} months",
            ["ComaEveryX0Years"] = ", every {0} years",
            ["ComaOnDayX0OfTheMonth"] = ", on day {0} of the month",
            ["ComaOnlyInX0"] = ", only in {0}",
            ["ComaOnlyOnX0"] = ", only on {0}",
            ["ComaOnThe"] = ", on the ",
            ["ComaOnTheLastDayOfTheMonth"] = ", on the last day of the month",
            ["ComaOnTheLastWeekdayOfTheMonth"] = ", on the last weekday of the month",
            ["ComaOnTheLastX0OfTheMonth"] = ", on the last {0} of the month",
            ["ComaOnTheX0OfTheMonth"] = ", on the {0} of the month",
            ["ComaX0ThroughX1"] = ", {0} through {1}",
            ["CommaDaysBeforeTheLastDayOfTheMonth"] = ", {0} days before the last day of the month",
            ["CommaStartingX0"] = ", starting {0}",
            ["EveryHour"] = "every hour",
            ["EveryMinute"] = "every minute",
            ["EveryMinuteBetweenX0AndX1"] = "Every minute between {0} and {1}",
            ["EverySecond"] = "every second",
            ["EveryX0Hours"] = "every {0} hours",
            ["EveryX0Minutes"] = "every {0} minutes",
            ["EveryX0Seconds"] = "every {0} seconds",
            ["Fifth"] = "fifth",
            ["First"] = "first",
            ["FirstWeekday"] = "first weekday",
            ["Fourth"] = "fourth",
            ["MinutesX0ThroughX1PastTheHour"] = "minutes {0} through {1} past the hour",
            ["PMPeriod"] = "PM",
            ["Second"] = "second",
            ["SecondsX0ThroughX1PastTheMinute"] = "seconds {0} through {1} past the minute",
            ["SpaceAnd"] = " and",
            ["SpaceAndSpace"] = " and ",
            ["SpaceX0OfTheMonth"] = " {0} of the month",
            ["Third"] = "third",
            ["WeekdayNearestDayX0"] = "weekday nearest day {0}",
        };

        public static string GetString(string name)
        {
            if (_strings.TryGetValue(name, out var result))
                return result;
            return null;
        }
    }
}