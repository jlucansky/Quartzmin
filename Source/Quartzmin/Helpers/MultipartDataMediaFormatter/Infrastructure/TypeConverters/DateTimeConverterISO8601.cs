#if NETFRAMEWORK

using System;
using System.ComponentModel;
using System.Globalization;

namespace MultipartDataMediaFormatter.Infrastructure.TypeConverters
{
    /// <summary>
    /// convert datetime to ISO 8601 format string
    /// </summary>
    internal class DateTimeConverterISO8601 : DateTimeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value != null && value is DateTime && destinationType == typeof (string))
            {
                return ((DateTime)value).ToString("O"); // ISO 8601
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
#endif