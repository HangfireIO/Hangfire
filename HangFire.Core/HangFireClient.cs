using System;
using System.Collections.Generic;

namespace HangFire
{
    /// <summary>
    /// Represents a top-level class for enqueuing jobs.
    /// </summary>
    public class HangFireClient : IDisposable
    {
        private readonly JobInvoker _jobInvoker;

        private static readonly HangFireClient Instance = new HangFireClient(
            JobInvoker.Current);

        static HangFireClient()
        {
        }

        public static string PerformAsync<TJob>()
            where TJob : HangFireJob
        {
            return PerformAsync<TJob>(null);
        }

        public static string PerformAsync<TJob>(object args)
            where TJob : HangFireJob
        {
            return PerformAsync(typeof(TJob), args);
        }

        public static string PerformAsync(Type jobType, object args = null)
        {
            return Instance.Async(jobType, args);
        }

        public static string PerformIn<TJob>(TimeSpan interval)
            where TJob : HangFireJob
        {
            return PerformIn<TJob>(interval, null);
        }

        public static string PerformIn<TJob>(TimeSpan interval, object args)
            where TJob : HangFireJob
        {
            return PerformIn(interval, typeof(TJob), args);
        }

        public static string PerformIn(TimeSpan interval, Type jobType, object args = null)
        {
            return Instance.In(interval, jobType, args);
        }

        private readonly RedisStorage _redis = new RedisStorage();

        internal HangFireClient(JobInvoker jobInvoker)
        {
            _jobInvoker = jobInvoker;
        }

        public string Async(Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }
            if (!typeof (HangFireJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit '{1}'.", jobType, typeof(HangFireJob)), 
                    "jobType");
            }

            var queueName = JobHelper.GetQueueName(jobType);

            var clientContext = new ClientContext();
            var descriptor = CreateDescriptor(jobType, args);

            descriptor.EnqueueAction = () =>
                {
                    lock (_redis)
                    {
                        _redis.EnqueueJob(queueName, descriptor.JobId, descriptor.Job);
                    }
                };

            _jobInvoker.EnqueueJob(clientContext, descriptor);

            return descriptor.JobId;
        }

        public string In(TimeSpan interval, Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }
            if (!typeof(HangFireJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit '{1}'.", jobType, typeof(HangFireJob)),
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

            var clientContext = new ClientContext();
            var descriptor = CreateDescriptor(jobType, args);

            var at = DateTime.UtcNow.Add(interval).ToTimestamp();

            descriptor.EnqueueAction = () =>
            {
                lock (_redis)
                {
                    _redis.ScheduleJob(descriptor.JobId, descriptor.Job, at);
                }
            };

            _jobInvoker.EnqueueJob(clientContext, descriptor);

            return descriptor.JobId;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        private ClientJobDescriptor CreateDescriptor(Type jobType, object jobArgs)
        {
            var job = new Dictionary<string, string>();
            var descriptor = new ClientJobDescriptor(GenerateId(), job);

            job["Type"] = jobType.AssemblyQualifiedName;
            job["Args"] = JsonHelper.Serialize(descriptor.SerializeProperties(jobArgs));

            return descriptor;
        }

        private string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
