namespace HangFire
{
    class JobProcessor
    {
        private readonly WorkerActivator _activator;

        public JobProcessor(WorkerActivator activator)
        {
            _activator = activator;
        }

        public void ProcessJob(string serializedJob)
        {
            var job = JsonHelper.Deserialize<Job>(serializedJob);

            using (var worker = _activator.CreateWorker(job.WorkerType))
            {
                worker.Args = job.Args;

                // TODO: server middleware
                worker.Perform();
            }
        }
    }
}
