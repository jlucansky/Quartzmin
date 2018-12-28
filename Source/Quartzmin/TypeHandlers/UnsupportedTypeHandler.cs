namespace Quartzmin.TypeHandlers
{
    [EmbeddedTypeHandlerResources(nameof(UnsupportedTypeHandler), Script = "")]
    public class UnsupportedTypeHandler : TypeHandlerBase
    {
        public string AssemblyQualifiedName { get; set; }

        public string StringValue { get; set; }

        public override bool CanHandle(object value) => true;

        public override object ConvertFrom(object value)
        {
            return StringValue;
        }
    }
}
