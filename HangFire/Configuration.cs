using System;

namespace HangFire
{
    public class Configuration
    {
        internal static Configuration Instance = new Configuration();

        public static void Configure(Action<Configuration> action)
        {
            action(Instance);
        }

        internal Configuration()
        {
            WorkerActivator = new WorkerActivator();

            RedisHost = "localhost";
            RedisPort = 6379;
            RedisPassword = null;
            RedisDb = 0;
        }

        public WorkerActivator WorkerActivator { get; set; }

        public string RedisHost { get; set; }
        public int RedisPort { get; set; }
        public string RedisPassword { get; set; }
        public long RedisDb { get; set; }
    }
}
