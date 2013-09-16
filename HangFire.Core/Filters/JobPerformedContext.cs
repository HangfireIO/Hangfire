using System;

namespace HangFire
{
    public class JobPerformedContext
    {
        public JobPerformedContext(
            WorkerContext workerContext, 
            ServerJobDescriptor jobDescriptor, 
            bool canceled, Exception exception)
        {
            WorkerContext = workerContext;
            JobDescriptor = jobDescriptor;
            Canceled = canceled;
            Exception = exception;
        }

        public bool Canceled { get; private set; }
        public WorkerContext WorkerContext { get; private set; }
        public ServerJobDescriptor JobDescriptor { get; private set; }
        public Exception Exception { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}