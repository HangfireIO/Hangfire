namespace HangFire
{
    class JobProcessor
    {
        public void ProcessJob(string serializedJob)
        {
            var job = JsonHelper.Deserialize<Job>(serializedJob);

            using (var worker = Factory.CreateWorker(job.WorkerType))
            {
                worker.Args = job.Args;

                // TODO: server middleware
                worker.Perform();
            }
        }
    }
}
