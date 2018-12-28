#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;

namespace MultipartDataMediaFormatter.Infrastructure.Logger
{
    public class FormDataConverterLogger : IFormDataConverterLogger
    {
        private Dictionary<string, List<LogErrorInfo>> Errors { get; set; }

        public FormDataConverterLogger()
        {
            Errors = new Dictionary<string, List<LogErrorInfo>>();
        }

        public void LogError(string errorPath, Exception exception)
        {
            AddError(errorPath, new LogErrorInfo(exception));
        }

        public void LogError(string errorPath, string errorMessage)
        {
            AddError(errorPath, new LogErrorInfo(errorMessage));
        }

        public List<LogItem> GetErrors()
        {
            return Errors.Select(m => new LogItem()
            {
                ErrorPath = m.Key,
                Errors = m.Value.Select(t => t).ToList()
            }).ToList();
        }
                
        public void EnsureNoErrors()
        {
            if (Errors.Any())
            {
                var errors = Errors
                    .Select(m => String.Format("{0}: {1}", m.Key, String.Join(". ", m.Value.Select(x => (x.ErrorMessage ?? (x.Exception != null ? x.Exception.Message : ""))))))
                    .ToList();

                string errorMessage = String.Join(" ", errors);

                throw new Exception(errorMessage);
            }
        }

        private void AddError(string errorPath, LogErrorInfo info)
        {
            List<LogErrorInfo> listErrors;
            if (!Errors.TryGetValue(errorPath, out listErrors))
            {
                listErrors = new List<LogErrorInfo>();
                Errors.Add(errorPath, listErrors);
            }
            listErrors.Add(info);
        }

        public class LogItem
        {
            public string ErrorPath { get; set; }
            public List<LogErrorInfo> Errors { get; set; }
        }

        public class LogErrorInfo
        {
            public string ErrorMessage { get; private set; }
            public Exception Exception { get; private set; }
            public bool IsException { get; private set; }

            public LogErrorInfo(string errorMessage)
            {
                ErrorMessage = errorMessage;
                IsException = false;
            }

            public LogErrorInfo(Exception exception)
            {
                Exception = exception;
                IsException = true;
            }
        }
    }
}
#endif