using System;
using HangFire.Server;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IServerExceptionFilter.OnServerException"/>
    /// method of the <see cref="IServerExceptionFilter"/> interface.
    /// </summary>
    public class ServerExceptionContext : PerformContext
    {
        internal ServerExceptionContext(
            PerformContext context, 
            Exception exception)
            : base(context)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets an exception that occured during the performance of the job.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="ServerExceptionContext"/>
        /// object handles an exception occured during the performance of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}