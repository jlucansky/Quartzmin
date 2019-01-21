using JsonSubTypes;
using Newtonsoft.Json;
using Quartzmin.TypeHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quartzmin
{
    public class TypeHandlerService
    {
        readonly Dictionary<Type, TypeHandlerDescriptor> _handlers = new Dictionary<Type, TypeHandlerDescriptor>();

        readonly Services _services;

        readonly JsonSubtypesConverterBuilder _builder;

        public DateTime LastModified { get; private set; }

        JsonSerializerSettings _jsonSerializerSettings = null;
        private JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                if (_jsonSerializerSettings == null)
                {
                    lock (this)
                    {
                        if (_jsonSerializerSettings == null)
                        {
                            var jss = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                            jss.Converters.Add(_builder.Build());
                            _jsonSerializerSettings = jss;
                        }
                    }
                }

                return _jsonSerializerSettings;
            }
        }

        class TypeHandlerDescriptor
        {
            public Type Type { get; set; }

            public string TypeId { get; set; }

            public Func<object, string> Render { get; set; }

            public TypeHandlerResourcesAttribute Resources { get; set; }
        }

        public TypeHandlerService(Services services)
        {
            _services = services;

            _builder = JsonSubtypesConverterBuilder.Of(typeof(TypeHandlerBase), nameof(TypeHandlerBase.TypeId));
            
            if (services?.Options?.StandardTypes != null)
            {
                foreach (var typeHandler in services.Options.StandardTypes.Select(x => x.GetType()).Distinct())
                    Register(typeHandler);
            }

            Register(typeof(UnsupportedTypeHandler));
        }

        public void Register(Type type)
        {
            if (!typeof(TypeHandlerBase).IsAssignableFrom(type))
                throw new ArgumentException("Type must inherit from " + nameof(TypeHandlerBase));

            var desc = new TypeHandlerDescriptor()
            {
                Type = type,
                TypeId = TypeHandlerBase.GetTypeId(type),
                Resources = TypeHandlerResourcesAttribute.GetResolved(type),
            };

            desc.Render = _services.Handlebars.Compile(desc.Resources.Template);

            _handlers.Add(type, desc);

            _builder.RegisterSubtype(type, desc.TypeId);


            _jsonSerializerSettings = null; // reset cached json converters

            LastModified = DateTime.UtcNow;
        }

        public TypeHandlerBase Deserialize(string str) => JsonConvert.DeserializeObject<TypeHandlerBase>(Encoding.UTF8.GetString(Convert.FromBase64String(str)), JsonSerializerSettings);

        public string Serialize(TypeHandlerBase typeHandler) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(typeHandler, JsonSerializerSettings)));

        public string Render(TypeHandlerBase typeHandler, object model)
        {
            if (_handlers.TryGetValue(typeHandler.GetType(), out var desc))
                return desc.Render(model);
            else
                throw new InvalidOperationException("Type handler not registered: " + typeHandler.GetType().FullName);
        }

        public Dictionary<string, string> GetScripts()
        {
            return _handlers.Values
                .Select(x => new { x.TypeId, x.Resources.Script })
                .ToArray()
                .Where(x => !string.IsNullOrWhiteSpace(x.Script))
                .ToDictionary(x => x.TypeId, x => x.Script);
        }
    }
}
