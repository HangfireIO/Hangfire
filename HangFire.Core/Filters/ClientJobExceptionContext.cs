using System;

namespace HangFire
{
    public class ClientJobExceptionContext
    {
        public ClientJobExceptionContext(ClientContext clientContext, Exception exception)
        {
            ClientContext = clientContext;
            Exception = exception;
        }

        public ClientContext ClientContext { get; set; }

        public Exception Exception { get; set; }
        public bool ExceptionHandled { get; set; }
    }
}