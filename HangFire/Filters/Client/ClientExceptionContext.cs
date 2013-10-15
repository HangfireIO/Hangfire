using System;

using HangFire.Client;

namespace HangFire.Filters
{
    public class ClientExceptionContext : CreateContext
    {
        public ClientExceptionContext(CreateContext createContext, Exception exception)
            : base(createContext)
        {
            Exception = exception;
        }

        public Exception Exception { get; set; }
        public bool ExceptionHandled { get; set; }
    }
}