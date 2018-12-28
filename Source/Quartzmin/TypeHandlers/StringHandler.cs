using System;

namespace Quartzmin.TypeHandlers
{
    [EmbeddedTypeHandlerResources(nameof(StringHandler))]
    public class StringHandler : TypeHandlerBase
    {
        public bool IsMultiline { get; set; }

        public override bool CanHandle(object value)
        {
            if (value is string str)
                return (str.IndexOf('\n') != -1) == IsMultiline;

            return false;
        }

        public override object ConvertFrom(object value)
        {
            if (value is string str)
            {
                if (IsMultiline == false)
                    return str.Substring(0, Math.Min(str.Length, 0x10000)); // for simple string field, constrain maximum length

                return str;
            }

            return null;
        }
    }
}
