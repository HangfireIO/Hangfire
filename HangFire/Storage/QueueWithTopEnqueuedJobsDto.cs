namespace HangFire.Storage
{
    public class QueueWithTopEnqueuedJobsDto
    {
        public string QueueName { get; set; }
        public EnqueuedJobDto[] FirstJobs { get; set; }
    }
}
