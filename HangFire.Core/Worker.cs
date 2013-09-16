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
        private readonly HangFireJobActivator _jobActivator;

        public Worker(string name, string workerName, JobInvoker jobInvoker, HangFireJobActivator jobActivator)
        {
            Logger = LogManager.GetLogger(name);
            _workerName = workerName;
            _jobInvoker = jobInvoker;
            _jobActivator = jobActivator;
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

            ServerJobDescriptor jobDescriptor = null;
            try
            {
                var workerContext = new WorkerContext(
                    "lalala", _workerName, "hahaha"); // TODO: use real values.

                jobDescriptor = new ServerJobDescriptor(_jobActivator, jobId, jobType, jobArgs);
                _jobInvoker.PerformJob(workerContext, jobDescriptor);
            }
            catch (Exception ex)
            {
                exception = ex;

                Logger.Error(String.Format(
                    "Failed to process the job '{0}': unexpected exception caught.",
                    jobId));
            }
            finally
            {
                if (jobDescriptor != null)
                {
                    jobDescriptor.Dispose();
                }
            }

            lock (Redis)
            {
                Redis.RetryOnRedisException(x => 
                    x.RemoveProcessingWorker(_workerName, jobId, exception));
            }
        }
    }
}