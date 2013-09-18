using System;

using HangFire.Server;

namespace HangFire.Filters
{
    public class JobPerformedContext : WorkerContext
    {
        internal JobPerformedContext(
            WorkerContext workerContext, 
            ServerJobDescriptor jobDescriptor, 
            bool canceled, Exception exception)
            : base(workerContext)
        {
            JobDescriptor = jobDescriptor;
            Canceled = canceled;
            Exception = exception;
        }

        public bool Canceled { get; private set; }
        public ServerJobDescriptor JobDescriptor { get; private set; }
        public Exception Exception { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}