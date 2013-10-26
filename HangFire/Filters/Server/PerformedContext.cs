using System;
using HangFire.Server;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IServerFilter.OnPerformed"/>
    /// method of the <see cref="IServerFilter"/> interface.
    /// </summary>
    public class PerformedContext : PerformContext
    {
        internal PerformedContext(
            PerformContext context, 
            bool canceled, 
            Exception exception)
            : base(context)
        {
            Canceled = canceled;
            Exception = exception;
        }

        /// <summary>
        /// Gets a value that indicates that this <see cref="PerformedContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; private set; }

        /// <summary>
        /// Gets an exception that occured during the performance of the job.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="PerformedContext"/>
        /// object handles an exception occured during the performance of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}