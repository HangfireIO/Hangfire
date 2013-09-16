using System;

namespace HangFire
{
    public class JobEnqueuedContext
    {
        public JobEnqueuedContext(
            ClientContext clientContext, 
            ClientJobDescriptor jobDescriptor,
            bool canceled, 
            Exception exception)
        {
            ClientContext = clientContext;
            JobDescriptor = jobDescriptor;
            Canceled = canceled;
            Exception = exception;
        }

        public ClientContext ClientContext { get; private set; }
        public ClientJobDescriptor JobDescriptor { get; private set; }
        public Exception Exception { get; private set; }
        public bool Canceled { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}