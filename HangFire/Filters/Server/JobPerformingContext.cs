using HangFire.Server;

namespace HangFire.Filters
{
    public class JobPerformingContext : WorkerContext
    {
        public JobPerformingContext(
            WorkerContext workerContext,
            ServerJobDescriptor jobDescriptor)
            : base(workerContext)
        {
            JobDescriptor = jobDescriptor;
        }

        public ServerJobDescriptor JobDescriptor { get; private set; }

        public bool Canceled { get; set; }
    }
}