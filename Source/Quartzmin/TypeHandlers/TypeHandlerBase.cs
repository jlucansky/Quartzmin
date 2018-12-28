using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Quartzmin.TypeHandlers
{
    public abstract class TypeHandlerBase
    {
        private string _name;

        /// <summary>
        /// Type Discriminator
        /// </summary>
        public string TypeId => GetTypeId(GetType());

        internal static string GetTypeId(Type type) => type.FullName;

        /// <summary>
        /// Unique name across <see cref="QuartzminOptions.StandardTypes"/>
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                DisplayName = DisplayName ?? _name;
            }
        }

        public string DisplayName { get; set; }

        public virtual string RenderView(Services services, object value)
        {
            return services.TypeHandlers.Render(this, new
            {
                Value = value,
                StringValue = ConvertToString(value),
                TypeHandler = this
            });
        }

        public virtual object ConvertFrom(Dictionary<string, object> formData)
        {
            return ConvertFrom(formData?.Values?.FirstOrDefault());
        }

        public abstract bool CanHandle(object value);

        /// <summary>
        /// If the value is expected type, just return the value. Every implementation should support conversion from String.
        /// </summary>
        public abstract object ConvertFrom(object value);

        /// <summary>
        /// Most of TypeHandlers support conversion from invariant string. Implement this method such as another TypeHandler can easily convert from this string.
        /// </summary>
        public virtual string ConvertToString(object value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public virtual bool IsValid(object value) => value != null;

        public override bool Equals(object obj)
        {
            return Name.Equals((obj as TypeHandlerBase)?.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
