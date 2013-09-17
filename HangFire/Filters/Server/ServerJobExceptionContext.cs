using System;

using HangFire.Server;

namespace HangFire.Filters
{
    public class ServerJobExceptionContext
    {
        public ServerJobExceptionContext(WorkerContext workerContext, Exception exception)
        {
            WorkerContext = workerContext;
            Exception = exception;
        }

        public WorkerContext WorkerContext { get; private set; }

        public Exception Exception { get; private set; }
        public bool ExceptionHandled { get; set; }
    }
}