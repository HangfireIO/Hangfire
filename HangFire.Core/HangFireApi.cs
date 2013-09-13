using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    public static class HangFireApi
    {
        private static readonly RedisStorage Redis = new RedisStorage();

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

        public static IEnumerable<WorkerDto> Workers()
        {
            lock (Redis)
            {
                return Redis.GetWorkers();
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
    }
}
