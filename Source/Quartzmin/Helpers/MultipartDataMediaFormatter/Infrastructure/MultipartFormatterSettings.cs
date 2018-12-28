#if NETFRAMEWORK

using System.Globalization;

namespace MultipartDataMediaFormatter.Infrastructure
{
    public class MultipartFormatterSettings
    {
        /// <summary>
        /// serialize byte array property as HttpFile when sending data if true or as indexed array if false
        /// (default value is "false)
        /// </summary>
        public bool SerializeByteArrayAsHttpFile { get; set; }

        /// <summary>
        /// add validation error "The value is required." if no value is present in request for non-nullable property if this parameter is "true"
        /// (default value is "false)
        /// </summary>
        public bool ValidateNonNullableMissedProperty { get; set; }

        private CultureInfo _CultureInfo;
        /// <summary>
        /// default is CultureInfo.CurrentCulture
        /// </summary>
        public CultureInfo CultureInfo
        {
            get { return _CultureInfo ?? CultureInfo.CurrentCulture; }
            set { _CultureInfo = value; }
        }
    }
}
#endif