using System;

using HangFire.Client;

namespace HangFire.Filters
{
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

        public Exception Exception { get; private set; }
        public bool Canceled { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}