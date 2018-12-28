namespace Quartzmin.TypeHandlers
{
    [EmbeddedTypeHandlerResources(nameof(BooleanHandler))]
    public class BooleanHandler : TypeHandlerBase
    {
        public override bool CanHandle(object value)
        {
            return value is bool;
        }

        public override object ConvertFrom(object value)
        {
            if (value is bool)
                return value;

            if (value is string str && bool.TryParse(str, out var result))
                return result;

            return null;
        }

        public override string ConvertToString(object value) => base.ConvertToString(value ?? false);
    }
}
