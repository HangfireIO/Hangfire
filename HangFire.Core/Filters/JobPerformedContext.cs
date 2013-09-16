using System;

namespace HangFire
{
    public class JobPerformedContext
    {
        public JobPerformedContext(
            WorkerContext workerContext, 
            HangFireJob jobInstance, 
            bool canceled, Exception exception)
        {
            WorkerContext = workerContext;
            JobInstance = jobInstance;
            Canceled = canceled;
            Exception = exception;
        }

        public bool Canceled { get; private set; }
        public WorkerContext WorkerContext { get; private set; }
        public HangFireJob JobInstance { get; private set; }
        public Exception Exception { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}