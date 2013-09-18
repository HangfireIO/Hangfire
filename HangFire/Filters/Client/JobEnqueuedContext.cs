using System;

using HangFire.Client;

namespace HangFire.Filters
{
    public class JobEnqueuedContext : ClientContext
    {
        internal JobEnqueuedContext(
            ClientContext clientContext, 
            ClientJobDescriptor jobDescriptor,
            bool canceled, 
            Exception exception)
            : base(clientContext)
        {
            JobDescriptor = jobDescriptor;
            Canceled = canceled;
            Exception = exception;
        }

        public ClientJobDescriptor JobDescriptor { get; private set; }
        public Exception Exception { get; private set; }
        public bool Canceled { get; private set; }

        public bool ExceptionHandled { get; set; }
    }
}