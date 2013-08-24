using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    public static class Storage
    {
        private static readonly RedisClient _client = new RedisClient();

        public static long ScheduledCount()
        {
            lock (_client)
            {
                long scheduled = 0;
                _client.TryToDo(x => scheduled = x.GetScheduledCount());
                return scheduled;
            }
        }

        public static long EnqueuedCount()
        {
            lock (_client)
            {
                long count = 0;
                _client.TryToDo(x => count = x.GetEnqueuedCount());
                return count;
            }
        }

        public static long SucceededCount()
        {
            lock (_client)
            {
                long count = 0;
                _client.TryToDo(x => count = x.GetSucceededCount());
                return count;
            }
        }

        public static long FailedCount()
        {
            lock (_client)
            {
                long count = 0;
                _client.TryToDo(x => count = x.GetFailedCount());
                return count;
            }
        }

        public static long ProcessingCount()
        {
            lock (_client)
            {
                long count = 0;
                _client.TryToDo(x => count = x.GetProcessingCount());
                return count;
            }
        }

        public static IEnumerable<QueueDto> Queues()
        {
            lock (_client)
            {
                IEnumerable<QueueDto> queues = Enumerable.Empty<QueueDto>();
                _client.TryToDo(x => queues = x.GetQueues());
                return queues;
            }
        }

        public static IEnumerable<DispatcherDto> Dispatchers()
        {
            lock (_client)
            {
                var dispatchers = Enumerable.Empty<DispatcherDto>();
                _client.TryToDo(x => dispatchers = x.GetDispatchers());
                return dispatchers;
            }
        }

        public static IList<ScheduleDto> Schedule()
        {
            lock (_client)
            {
                IList<ScheduleDto> schedule = new List<ScheduleDto>();
                _client.TryToDo(x => schedule = x.GetSchedule());
                return schedule;
            }
        }

        public static Dictionary<string, long> SucceededByDatesCount()
        {
            lock (_client)
            {
                var count = new Dictionary<string, long>();
                _client.TryToDo(x => count = x.GetSucceededByDatesCount());
                return count;
            }
        }

        public static Dictionary<string, long> FailedByDatesCount()
        {
            lock (_client)
            {
                var count = new Dictionary<string, long>();
                _client.TryToDo(x => count = x.GetFailedByDatesCount());
                return count;
            }
        }

        public static IList<ServerDto> Servers()
        {
            lock (_client)
            {
                IList<ServerDto> servers = new List<ServerDto>();
                _client.TryToDo(x => servers = x.GetServers());
                return servers;
            }
        }

        public static IList<FailedJobDto> FailedJobs()
        {
            lock (_client)
            {
                IList<FailedJobDto> failed = new List<FailedJobDto>();
                _client.TryToDo(x => failed = x.GetFailedJobs());
                return failed;
            }
        }

        public static IList<SucceededJobDto> SucceededJobs()
        {
            lock (_client)
            {
                IList<SucceededJobDto> succeeded = new List<SucceededJobDto>();
                _client.TryToDo(x => succeeded = x.GetSucceededJobs());
                return succeeded;
            }
        }

        public static Dictionary<DateTime, long> HourlySucceededJobs()
        {
            lock (_client)
            {
                var result = new Dictionary<DateTime, long>();
                _client.TryToDo(x => result = x.GetHourlySucceededCount());
                return result;
            }
        }

        public static Dictionary<DateTime, long> HourlyFailedJobs()
        {
            lock (_client)
            {
                var result = new Dictionary<DateTime, long>();
                _client.TryToDo(x => result = x.GetHourlyFailedCount());
                return result;
            }
        }
    }
}
