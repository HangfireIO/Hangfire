using System;
using System.Collections.Generic;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    internal class JobClient : IDisposable
    {
        private readonly JobCreator _jobCreator = JobCreator.Current;
        private readonly IRedisClient _redis = RedisFactory.GetManager().GetClient();

        public string Async(Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }
            if (!typeof (BackgroundJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit '{1}'.", jobType, typeof(BackgroundJob)), 
                    "jobType");
            }

            var queue = JobHelper.GetQueue(jobType);

            var jobId = GenerateId();

            var state = new EnqueuedState(jobId, "Enqueued by the Сlient", queue);
            var job = CreateJob(jobType, args);

            var context = new CreateContext(
                new ClientJobDescriptor(_redis, jobId, job, state));
            
            _jobCreator.CreateJob(context);

            return jobId;
        }

        public string In(TimeSpan interval, Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }
            if (!typeof(BackgroundJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit '{1}'.", jobType, typeof(BackgroundJob)),
                    "jobType");
            }

            if (interval != interval.Duration())
            {
                throw new ArgumentOutOfRangeException("interval", "Interval value can not be negative.");
            }

            if (interval.Equals(TimeSpan.Zero))
            {
                return Async(jobType, args);
            }

            var jobId = GenerateId();

            var state = new ScheduledState(jobId, "Scheduled by the Client", DateTime.UtcNow.Add(interval));
            var job = CreateJob(jobType, args);

            var context = new CreateContext(
                new ClientJobDescriptor(_redis, jobId, job, state));

            _jobCreator.CreateJob(context);

            return jobId;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        private static Dictionary<string, string> CreateJob(
            Type jobType, object jobArgs)
        {
            var job = new Dictionary<string, string>();
            job["Type"] = jobType.AssemblyQualifiedName;
            job["Args"] = JobHelper.ToJson(ClientJobDescriptor.SerializeProperties(jobArgs));
            job["CreatedAt"] = JobHelper.ToStringTimestamp(DateTime.UtcNow);

            return job;
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
