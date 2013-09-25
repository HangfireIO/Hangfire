using System;
using System.Collections.Generic;

using HangFire.Storage;

namespace HangFire
{
    public static class JobStorage
    {
        private static readonly Lazy<RedisStorage> _lazyRedis
            = new Lazy<RedisStorage>(() => new RedisStorage());

        static JobStorage()
        {
            Configuration = new JobStorageConfiguration();
        }

        /// <summary>
        /// Gets the current HangFire configuration.
        /// </summary>
        public static JobStorageConfiguration Configuration { get; private set; }
        private static RedisStorage Redis
        {
            get { return _lazyRedis.Value; }
        }

        /// <summary>
        /// Runs specified configuration action to configure HangFire.
        /// </summary>
        /// <param name="action">Configuration action.</param>
        public static void Configure(Action<JobStorageConfiguration> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            action(Configuration);
        }

        public static long ScheduledCount()
        {
            lock (Redis)
            {
                return Redis.GetScheduledCount();
            }
        }

        public static long EnqueuedCount()
        {
            lock (Redis)
            {
                return Redis.GetEnqueuedCount();
            }
        }

        public static long SucceededCount()
        {
            lock (Redis)
            {
                return Redis.GetSucceededCount();
            }
        }

        public static long FailedCount()
        {
            lock (Redis)
            {
                return Redis.GetFailedCount();
            }
        }

        public static long ProcessingCount()
        {
            lock (Redis)
            {
                return Redis.GetProcessingCount();
            }
        }

        public static IEnumerable<QueueDto> Queues()
        {
            lock (Redis)
            {
                return Redis.GetQueues();
            }
        }

        public static IEnumerable<ProcessingJobDto> ProcessingJobs()
        {
            lock (Redis)
            {
                return Redis.GetProcessingJobs();
            }
        }

        public static IList<ScheduleDto> Schedule()
        {
            lock (Redis)
            {
                return Redis.GetSchedule();
            }
        }

        public static Dictionary<string, long> SucceededByDatesCount()
        {
            lock (Redis)
            {
                return Redis.GetSucceededByDatesCount();
            }
        }

        public static Dictionary<string, long> FailedByDatesCount()
        {
            lock (Redis)
            {
                return Redis.GetFailedByDatesCount();
            }
        }

        public static IList<ServerDto> Servers()
        {
            lock (Redis)
            {
                return Redis.GetServers();
            }
        }

        public static IList<FailedJobDto> FailedJobs()
        {
            lock (Redis)
            {
                return Redis.GetFailedJobs();
            }
        }

        public static IList<SucceededJobDto> SucceededJobs()
        {
            lock (Redis)
            {
                return Redis.GetSucceededJobs();
            }
        }

        public static IList<QueueWithTopEnqueuedJobsDto> EnqueuedJobs()
        {
            lock (Redis)
            {
                return Redis.GetEnqueuedJobs();
            }
        }

        public static Dictionary<DateTime, long> HourlySucceededJobs()
        {
            lock (Redis)
            {
                return Redis.GetHourlySucceededCount();
            }
        }

        public static Dictionary<DateTime, long> HourlyFailedJobs()
        {
            lock (Redis)
            {
                return Redis.GetHourlyFailedCount();
            }
        }

        public static bool RetryJob(string jobId)
        {
            lock (Redis)
            {
                return Redis.RetryJob(jobId);
            }
        }

        public static bool RemoveJob(string jobId)
        {
            lock (Redis)
            {
                return Redis.RemoveFailedJob(jobId);
            }
        }
    }
}
