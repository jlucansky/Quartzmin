using Quartzmin.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Quartzmin.TypeHandlers
{
    [EmbeddedTypeHandlerResources(nameof(FileHandler))]
    public class FileHandler : TypeHandlerBase
    {
        public override bool CanHandle(object value)
        {
            return value is byte[];
        }

        public override object ConvertFrom(Dictionary<string, object> formData)
        {
            if (formData.TryGetValue("data-map[old-file-value]", out var oldData))
                return ConvertFrom(Convert.FromBase64String((string)oldData));
            else
                return base.ConvertFrom(formData);
        }

        public override object ConvertFrom(object value)
        {
            if (value is byte[])
                return value;

            if (value is string str)
                return Encoding.UTF8.GetBytes(str);

            if (value is FormFile file)
            {
                return file.GetBytes();
            }

            return null;
        }

        public override string ConvertToString(object value)
        {
            if (value is byte[] bytes)
            {
                var str = Encoding.UTF8.GetString(bytes);
                if (HasBinaryContent(str) == false)
                    return str;
            }

            return null;
        }

        bool HasBinaryContent(string content)
        {
            return content.Take(1024).Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t');
        }
    }
}
