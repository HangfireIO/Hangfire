using System;

using HangFire.Client;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IClientFilter.OnCreated"/> 
    /// method of the <see cref="IClientFilter"/> interface.
    /// </summary>
    public class CreatedContext : CreateContext
    {
        internal CreatedContext(
            CreateContext context, 
            bool canceled, 
            Exception exception)
            : base(context)
        {
            Canceled = canceled;
            Exception = exception;
        }

        /// <summary>
        /// Gets an exception that occured during the creation of the job.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets a value that indicates that this <see cref="CreatedContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="CreatedContext"/>
        /// object handles an exception occured during the creation of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}