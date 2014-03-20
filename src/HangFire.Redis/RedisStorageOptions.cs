using System;

namespace HangFire.Redis
{
    public class RedisStorageOptions
    {
        public RedisStorageOptions()
        {
            PollInterval = TimeSpan.FromSeconds(15);
            ConnectionPoolSize = 50;
        }

        public TimeSpan PollInterval { get; set; }
        public int ConnectionPoolSize { get; set; }
    }
}
