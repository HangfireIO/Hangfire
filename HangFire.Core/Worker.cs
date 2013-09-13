using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace HangFire
{
    internal class Worker
    {
        private static readonly RedisStorage Redis = new RedisStorage();

        protected readonly ILog Logger;

        private readonly string _workerName;
        private readonly HangFireJobActivator _jobActivator;

        private readonly JobInvoker _invoker;

        public Worker(string name, string workerName, HangFireJobActivator jobActivator)
        {
            Logger = LogManager.GetLogger(name);
            _workerName = workerName;
            _jobActivator = jobActivator;

            _invoker = new JobInvoker(
                HangFireConfiguration.Current.ServerFilters);
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
            HangFireJob jobInstance = null;
            try
            {
                try
                {
                    var type = Type.GetType(jobType, true, true);
                    jobInstance = _jobActivator.ActivateJob(type);
                }
                catch (Exception ex)
                {
                    throw new JobActivationException(
                        String.Format(
                            "An exception occured while trying to activate a job with the type '{0}'",
                            jobType),
                        ex);
                }

                jobInstance.JobId = jobId;
                jobInstance.Redis = Redis;

                _invoker.InvokeJob(jobInstance, jobArgs);
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
                var disposable = jobInstance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
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