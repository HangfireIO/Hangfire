using System;

using ServiceStack.Redis;

namespace HangFire
{
    internal class RedisClient : IDisposable
    {
        private IRedisClient _connection;

        public RedisClient()
        {
            _connection = CreateConnection();
        }

        public IRedisClient Connection
        {
            get
            {
                return _connection;
            }
        }

        public void Reconnect()
        {
            _connection = CreateConnection();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        private static IRedisClient CreateConnection()
        {
            return new ServiceStack.Redis.RedisClient("127.0.0.1");
        }
    }
}
