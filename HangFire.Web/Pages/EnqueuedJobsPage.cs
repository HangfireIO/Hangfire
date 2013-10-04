namespace HangFire.Web.Pages
{
    partial class EnqueuedJobsPage
    {
        public EnqueuedJobsPage(string queueName)
        {
            QueueName = queueName;
        }

        public string QueueName { get; private set; }
    }
}
