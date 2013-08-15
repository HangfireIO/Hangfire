using System;

using ServiceStack.Redis;

namespace HangFire
{
    internal class RedisClient : IDisposable
    {
        private IRedisClient _connection;

        public IRedisClient Connection
        {
            get
            {
                if (_connection == null)
                {
                    Reconnect();
                }

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
            var config = Configuration.Instance;
            return new ServiceStack.Redis.RedisClient(config.RedisHost, config.RedisPort, config.RedisPassword, config.RedisDb);
        }
    }
}
