using System;

namespace HangFire
{
    public class JobExceptionContext
    {
        public JobExceptionContext(WorkerContext workerContext, Exception exception)
        {
            WorkerContext = workerContext;
            Exception = exception;
        }

        public WorkerContext WorkerContext { get; private set; }

        public Exception Exception { get; private set; }
        public bool ExceptionHandled { get; set; }
    }
}