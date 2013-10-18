using System;
using ServiceStack.Redis;

namespace HangFire
{
    public static class RedisFactory
    {
        private static readonly Lazy<IRedisClientsManager> _manager
            = new Lazy<IRedisClientsManager>(() => String.IsNullOrEmpty(Password)
                ? new PooledRedisClientManager(
                    _db, String.Format("{0}:{1}", _host, _port)) 
                : new PooledRedisClientManager(
                    _db, String.Format("{0}:{1}@{2}", _host, _port, Password)));

        private static string _host = RedisNativeClient.DefaultHost;
        private static int _port = RedisNativeClient.DefaultPort;
        private static long _db = RedisNativeClient.DefaultDb;

        /// <summary>
        /// Gets or sets Redis hostname. Default: "localhost"
        /// </summary>
        public static string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        /// <summary>
        /// Gets or sets Redis port. Default: 6379
        /// </summary>
        public static int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// Gets or sets Redis password. Default: null
        /// </summary>
        public static string Password { get; set; }

        /// <summary>
        /// Gets or sets Redis database number. Default: 0
        /// </summary>
        public static long Db
        {
            get { return _db; }
            set { _db = value; }
        }

        public static IRedisClient CreateClient()
        {
            return new RedisClient(
                Host, 
                Port,
                Password,
                Db);
        }

        public static IRedisClientsManager GetManager()
        {
            return _manager.Value;
        }
    }
}
