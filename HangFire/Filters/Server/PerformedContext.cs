using System;

namespace HangFire.Filters
{
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

        public bool Canceled { get; private set; }
        public Exception Exception { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}