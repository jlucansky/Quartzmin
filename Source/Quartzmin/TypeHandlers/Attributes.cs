using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Quartzmin.TypeHandlers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TypeHandlerResourcesAttribute : Attribute
    {
        public string Template { get; set; }
        public string Script { get; set; }

        public static TypeHandlerResourcesAttribute GetResolved(Type type)
        {
            var attr = type.GetCustomAttribute<TypeHandlerResourcesAttribute>(inherit: true)
                ?? throw new ArgumentException(type.FullName + " missing attribute " + nameof(TypeHandlerResourcesAttribute));
            attr.Resolve();
            return attr;
        }

        protected virtual void Resolve() { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class EmbeddedTypeHandlerResourcesAttribute : TypeHandlerResourcesAttribute
    {
        /// <summary>
        /// Should override when used in another assembly.
        /// </summary>
        protected virtual Assembly Assembly => Assembly.GetExecutingAssembly();
        /// <summary>
        /// Should override when used in another assembly.
        /// </summary>
        protected virtual string Namespace => typeof(EmbeddedTypeHandlerResourcesAttribute).Namespace;

        protected override void Resolve()
        {
            Script = Script ?? GetManifestResourceString($"{_name}.js");
            Template = Template ?? GetManifestResourceString($"{_name}.hbs");
        }

        private readonly string _name;
        public EmbeddedTypeHandlerResourcesAttribute(string name)
        {
            _name = name;
        }

        protected string GetManifestResourceString(string name)
        {
            string fullName = $"{Namespace}.{name}";
            using (var stream = Assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Embedded resource not found: " + fullName + " in assembly: " + Assembly.FullName);

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }
    }
}
