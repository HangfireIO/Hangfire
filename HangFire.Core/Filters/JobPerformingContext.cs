namespace HangFire
{
    public class JobPerformingContext
    {
        public JobPerformingContext(
            WorkerContext workerContext, 
            HangFireJob jobInstance)
        {
            WorkerContext = workerContext;
            JobInstance = jobInstance;
        }

        public WorkerContext WorkerContext { get; private set; }
        public HangFireJob JobInstance { get; private set; }

        public bool Canceled { get; set; }
    }
}