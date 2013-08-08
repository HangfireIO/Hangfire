using System;
using ServiceStack.Logging;

namespace HangFire
{
    class JobProcessor
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(JobProcessor));

        public void ProcessJob(string serializedJob)
        {
            try
            {
                var job = JsonHelper.Deserialize<Job>(serializedJob);

                using (var worker = Factory.CreateWorker(job.WorkerType))
                {
                    worker.Args = job.Args;

                    // TODO: server middleware
                    worker.Perform();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to process the job: unexpected exception caught. Job JSON:"
                    + Environment.NewLine
                    + serializedJob,
                    ex);
            }
            finally
            {
                
            }
        }
    }
}
