#if NETFRAMEWORK

using System;
using System.ComponentModel;
using System.Globalization;

namespace MultipartDataMediaFormatter.Infrastructure.TypeConverters
{
    public class FromStringConverterAdapter
    {
        private readonly Type Type;
        private readonly TypeConverter TypeConverter;
        public FromStringConverterAdapter(Type type, TypeConverter typeConverter)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");

            Type = type;
            TypeConverter = typeConverter;
        }

        public object ConvertFromString(string src, CultureInfo culture)
        {
            var isUndefinedNullable = Nullable.GetUnderlyingType(Type) != null && src == "undefined";
            if (isUndefinedNullable)
                return null;

            return TypeConverter.ConvertFromString(null, culture, src);
        }
    }
}
#endif