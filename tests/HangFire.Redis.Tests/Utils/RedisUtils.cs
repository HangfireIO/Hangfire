using System;
using ServiceStack.Redis;

namespace HangFire.Redis.Tests
{
    public static class RedisUtils
    {
        private const string HostVariable = "HangFire_Redis_Host";
        private const string PortVariable = "HangFire_Redis_Port";
        private const string DbVariable = "HangFire_Redis_Db";

        private const string DefaultHost = "localhost";
        private const int DefaultPort = 6379;
        private const int DefaultDb = 1;

        public static IRedisClient CreateClient()
        {
            return new RedisClient(GetHost(), GetPort(), db: GetDb());
        }

        public static string GetHostAndPort()
        {
            return String.Format("{0}:{1}", GetHost(), GetPort());
        }

        public static string GetHost()
        {
            return Environment.GetEnvironmentVariable(HostVariable)
                   ?? DefaultHost;
        }

        public static int GetPort()
        {
            var portValue = Environment.GetEnvironmentVariable(PortVariable);
            return portValue != null ? int.Parse(portValue) : DefaultPort;
        }

        public static int GetDb()
        {
            var dbValue = Environment.GetEnvironmentVariable(DbVariable);
            return dbValue != null ? int.Parse(dbValue) : DefaultDb;
        }
    }
}
