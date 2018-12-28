#if NETFRAMEWORK

using System;

namespace MultipartDataMediaFormatter.Infrastructure.Logger
{
    public interface IFormDataConverterLogger
    {
        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="errorPath">The path to the member for which the error is being logged.</param>
        /// <param name="exception">The exception to be logged.</param>
        void LogError(string errorPath, Exception exception);

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="errorPath">The path to the member for which the error is being logged.</param>
        /// <param name="errorMessage">The error message to be logged.</param>
        void LogError(string errorPath, string errorMessage);

        /// <summary>
        /// throw exception if errors found
        /// </summary>
        void EnsureNoErrors();
    }
}
#endif