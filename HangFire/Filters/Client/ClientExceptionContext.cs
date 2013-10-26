using System;

using HangFire.Client;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IClientExceptionFilter.OnClientException"/>
    /// method of the <see cref="IClientExceptionFilter"/> interface.
    /// </summary>
    public class ClientExceptionContext : CreateContext
    {
        internal ClientExceptionContext(CreateContext createContext, Exception exception)
            : base(createContext)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets an exception that occured during the creation of the job.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="ClientExceptionContext"/>
        /// object handles an exception occured during the creation of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}