using System;

using HangFire.Client;

namespace HangFire.Filters
{
    public class ClientJobExceptionContext : ClientContext
    {
        public ClientJobExceptionContext(ClientContext clientContext, Exception exception)
            : base(clientContext)
        {
            Exception = exception;
        }

        public Exception Exception { get; set; }
        public bool ExceptionHandled { get; set; }
    }
}