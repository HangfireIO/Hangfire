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
    }
}
