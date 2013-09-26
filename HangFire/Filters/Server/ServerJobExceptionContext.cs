using System;

using HangFire.Server;

namespace HangFire.Filters
{
    public class ServerJobExceptionContext : WorkerContext
    {
        public ServerJobExceptionContext(
            WorkerContext workerContext, 
            ServerJobDescriptor jobDescriptor,
            Exception exception)
            : base(workerContext)
        {
            JobDescriptor = jobDescriptor;
            Exception = exception;
        }

        public ServerJobDescriptor JobDescriptor { get; set; }

        public Exception Exception { get; private set; }
        public bool ExceptionHandled { get; set; }
    }
}