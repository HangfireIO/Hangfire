using System;
using System.IO;
using System.Threading;

using ServiceStack.Redis;

namespace HangFire
{
    internal class RedisClient : IDisposable
    {
        private IRedisClient _connection;
        private readonly TimeSpan _reconnectTimeout = TimeSpan.FromSeconds(5);

        private readonly Configuration _config = Configuration.Instance;

        public bool TryToDo(Action<RedisStorage> action, bool throwOnError = false)
        {
            try
            {
                var connection = GetConnection();
                action(new RedisStorage(connection));
            }
            catch (IOException)
            {
                _connection = null;

                if (throwOnError)
                {  
                    throw;
                }

                Thread.Sleep(_reconnectTimeout);
            }
            catch (RedisException)
            {
                _connection = null;

                if (throwOnError)
                {
                    throw;
                }

                Thread.Sleep(_reconnectTimeout);
            }

            return _connection != null;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        private IRedisClient GetConnection()
        {
            return _connection ?? (_connection = CreateConnection());
        }

        private IRedisClient CreateConnection()
        {
            return new ServiceStack.Redis.RedisClient(_config.RedisHost, _config.RedisPort, _config.RedisPassword, _config.RedisDb);
        }
    }
}
