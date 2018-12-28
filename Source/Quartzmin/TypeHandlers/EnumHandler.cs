using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Quartzmin.TypeHandlers
{
    public class EnumHandler : OptionSetHandler
    {
        public Type EnumType { get; set; }

        public EnumHandler() { }
        public EnumHandler(Type enumType)
        {
            EnumType = enumType;
            Name = EnumType.FullName;
            DisplayName = EnumType.Name;
        }

        public override bool CanHandle(object value)
        {
            if (value == null)
                return false;

            return EnumType.IsAssignableFrom(value.GetType());
        }

        public override object ConvertFrom(object value)
        {
            if (value == null)
                return null;

            if (EnumType.IsAssignableFrom(value.GetType()))
                return value;

            if (value is string str)
            {
                try
                {
                    return Enum.Parse(EnumType, str, true);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        string GetDisplayName(string enumValue)
        {
            return EnumType?
                .GetMember(enumValue)?.First()?
                .GetCustomAttribute<DisplayAttribute>()?
                .Name ?? enumValue;
        }

        public override KeyValuePair<string, string>[] GetItems()
        {
            return Enum.GetNames(EnumType)
                .Select(x => new KeyValuePair<string, string>(x, GetDisplayName(x)))
                .ToArray();
        }
    }
}
