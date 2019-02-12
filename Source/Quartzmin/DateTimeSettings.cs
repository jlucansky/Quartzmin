namespace Quartzmin
{
    internal static class DateTimeSettings
    {
        public static string DefaultDateFormat { get; set; } = "MM/dd/yyyy";
        public static string DefaultTimeFormat { get; set; } = "HH:mm:ss";
        public static bool UseLocalTime { get; set; } = false;
    }
}
