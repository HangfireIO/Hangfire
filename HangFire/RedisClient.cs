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

        public void TryToDo(Action<IRedisClient> action, bool reconnectOnNextUse = false)
        {
            try
            {
                var connection = GetConnection();
                action(connection);
            }
            catch (IOException)
            {
                if (reconnectOnNextUse)
                {
                    _connection = null;
                    throw;
                }

                Thread.Sleep(_reconnectTimeout);
                Reconnect();
            }
            catch (RedisException)
            {
                if (reconnectOnNextUse)
                {
                    _connection = null;
                    throw;
                }

                Thread.Sleep(_reconnectTimeout);
                Reconnect();
            }
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
            if (_connection == null)
            {
                Reconnect();
            }

            return _connection;
        }

        private void Reconnect()
        {
            _connection = CreateConnection();
        }

        private IRedisClient CreateConnection()
        {
            return new ServiceStack.Redis.RedisClient(_config.RedisHost, _config.RedisPort, _config.RedisPassword, _config.RedisDb);
        }
    }
}
