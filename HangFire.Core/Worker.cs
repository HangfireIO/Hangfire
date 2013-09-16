using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace HangFire
{
    internal class Worker
    {
        public static readonly RedisStorage Redis = new RedisStorage();

        protected readonly ILog Logger;

        private readonly string _workerName;
        private readonly JobInvoker _jobInvoker;

        public Worker(string name, string workerName, JobInvoker jobInvoker)
        {
            Logger = LogManager.GetLogger(name);
            _workerName = workerName;
            _jobInvoker = jobInvoker;
        }

        public virtual void Process(string jobId)
        {
            string jobType = null; 
            Dictionary<string, string> jobArgs = null;

            lock (Redis)
            {
                Redis.RetryOnRedisException(x =>
                    {
                        x.GetJobTypeAndArgs(jobId, out jobType, out jobArgs);
                        // TODO: what if the job doesn't exists?
                        x.AddProcessingWorker(_workerName, jobId);
                    });
            }

            Exception exception = null;
            
            try
            {
                var workerContext = new WorkerContext(
                    "lalala", _workerName, jobId, jobType, jobArgs);

                _jobInvoker.PerformJob(workerContext);
            }
            catch (Exception ex)
            {
                exception = ex;

                Logger.Error(String.Format(
                    "Failed to process the job '{0}': unexpected exception caught.",
                    jobId));
            }

            lock (Redis)
            {
                Redis.RetryOnRedisException(x => 
                    x.RemoveProcessingWorker(_workerName, jobId, exception));
            }
        }
    }
}