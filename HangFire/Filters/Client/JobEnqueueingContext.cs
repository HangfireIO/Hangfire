using HangFire.Client;

namespace HangFire.Filters
{
    public class JobEnqueueingContext : ClientContext
    {
        internal JobEnqueueingContext(
            ClientContext clientContext,
            ClientJobDescriptor jobDescriptor)
            : base(clientContext)
        {
            JobDescriptor = jobDescriptor;
        }

        public ClientJobDescriptor JobDescriptor { get; private set; }

        public bool Canceled { get; set; }
    }
}