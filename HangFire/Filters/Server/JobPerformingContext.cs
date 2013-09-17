using HangFire.Server;

namespace HangFire.Filters
{
    public class JobPerformingContext
    {
        public JobPerformingContext(
            WorkerContext workerContext, 
            ServerJobDescriptor jobDescriptor)
        {
            WorkerContext = workerContext;
            JobDescriptor = jobDescriptor;
        }

        public WorkerContext WorkerContext { get; private set; }
        public ServerJobDescriptor JobDescriptor { get; private set; }

        public bool Canceled { get; set; }
    }
}