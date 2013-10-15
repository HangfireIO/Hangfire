using System;

namespace HangFire.Filters
{
    public class ServerExceptionContext : PerformContext
    {
        public ServerExceptionContext(
            PerformContext context, 
            Exception exception)
            : base(context)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
        public bool ExceptionHandled { get; set; }
    }
}