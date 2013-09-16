namespace HangFire
{
    public class JobEnqueueingContext
    {
        public JobEnqueueingContext(
            ClientContext clientContext,
            ClientJobDescriptor jobDescriptor)
        {
            ClientContext = clientContext;
            JobDescriptor = jobDescriptor;
        }

        public ClientContext ClientContext { get; private set; }
        public ClientJobDescriptor JobDescriptor { get; private set; }

        public bool Canceled { get; set; }
    }
}