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
    }
}
