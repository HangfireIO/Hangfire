using System;
using System.Net.Sockets;

using BookSleeve;

namespace HangFire
{
    internal class RedisClient : IDisposable
    {
        private const string RedisConnectionFailed = "Redis connection failed.";
        private RedisConnection _connection;

        private readonly object _syncConnectionLock = new object();

        public RedisClient()
        {
            _connection = CreateConnection();
        }

        public void Dispose()
        {
            lock (_syncConnectionLock)
            {
                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }
            }
        }

        private static RedisConnection CreateConnection()
        {
            return new RedisConnection("127.0.0.1");
        }

        public RedisConnection GetConnection()
        {
            lock (_syncConnectionLock)
            {
                if (_connection == null)
                {
                    _connection = CreateConnection();
                }

                if (_connection.State == RedisConnectionBase.ConnectionState.Closing 
                    || _connection.State == RedisConnectionBase.ConnectionState.Closed)
                {
                    try
                    {
                        _connection = CreateConnection();
                    }
                    catch (Exception ex)
                    {
                        throw new RedisException(RedisConnectionFailed, ex);
                    }
                }

                if (_connection.State == RedisConnectionBase.ConnectionState.New)
                {
                    try
                    {
                        var openAsync = _connection.Open();
                        _connection.Wait(openAsync);
                    }
                    catch (SocketException ex)
                    {
                        throw new RedisException(RedisConnectionFailed, ex);
                    }
                }

                return _connection;
            }
        }
    }
}
