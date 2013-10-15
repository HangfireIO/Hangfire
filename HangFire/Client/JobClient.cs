using System;
using System.Collections.Generic;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    internal class JobClient : IDisposable
    {
        private readonly JobCreator _jobCreator = JobCreator.Current;
        private readonly IRedisClient _redis = RedisFactory.Create();

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

            var descriptor = CreateDescriptor(jobType, args);
            var context = new CreateContext(descriptor);

            descriptor.EnqueueAction = () =>
                {
                    lock (_redis)
                    {
                        CreateJob(descriptor.JobId, descriptor.Job);
                        JobState.Apply(
                            _redis, 
                            new EnqueuedState(descriptor.JobId, "Enqueued by the Сlient", queue));
                    }
                };

            _jobCreator.CreateJob(context);

            return descriptor.JobId;
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

            var descriptor = CreateDescriptor(jobType, args);
            var context = new CreateContext(descriptor);

            var at = DateTime.UtcNow.Add(interval);

            descriptor.EnqueueAction = () =>
            {
                lock (_redis)
                {
                    CreateJob(descriptor.JobId, descriptor.Job);

                    JobState.Apply(_redis, new ScheduledState(
                        descriptor.JobId, 
                        "Scheduled by the Client",
                        at));
                }
            };

            _jobCreator.CreateJob(context);

            return descriptor.JobId;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        private static ClientJobDescriptor CreateDescriptor(Type jobType, object jobArgs)
        {
            var job = new Dictionary<string, string>();
            var descriptor = new ClientJobDescriptor(GenerateId(), job);

            job["Type"] = jobType.AssemblyQualifiedName;
            job["Args"] = JobHelper.ToJson(ClientJobDescriptor.SerializeProperties(jobArgs));

            return descriptor;
        }

        private void CreateJob(string id, Dictionary<string, string> properties)
        {
            _redis.SetRangeInHash(
                String.Format("hangfire:job:{0}", id),
                properties);
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
