#if NETFRAMEWORK

using System;
using System.Net.Http.Formatting;

namespace MultipartDataMediaFormatter.Infrastructure.Logger
{
    internal class FormatterLoggerAdapter : IFormDataConverterLogger
    {
        private IFormatterLogger FormatterLogger { get; set; }

        public FormatterLoggerAdapter(IFormatterLogger formatterLogger)
        {
            if(formatterLogger == null)
                throw new ArgumentNullException("formatterLogger");
            FormatterLogger = formatterLogger;
        }

        public void LogError(string errorPath, Exception exception)
        {
            FormatterLogger.LogError(errorPath, exception);
        }

        public void LogError(string errorPath, string errorMessage)
        {
            FormatterLogger.LogError(errorPath, errorMessage);
        }

        public void EnsureNoErrors() 
        {
            //nothing to do
        }
    }
}
#endif