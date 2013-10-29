using System;
using ServiceStack.Redis;

namespace HangFire
{
    public static class RedisFactory
    {
        private static readonly Lazy<IRedisClientsManager> _pooledManager;
        private static readonly Lazy<IRedisClientsManager> _basicManager;

        static RedisFactory()
        {
            Host = String.Format("{0}:{1}", RedisNativeClient.DefaultHost, RedisNativeClient.DefaultPort);
            Db = (int) RedisNativeClient.DefaultDb;

            _pooledManager = new Lazy<IRedisClientsManager>(
                () => new PooledRedisClientManager(Db, Host));
            
            _basicManager = new Lazy<IRedisClientsManager>(() => 
                new BasicRedisClientManager(Db, Host));
        }

        /// <summary>
        /// Gets or sets Redis hostname. Default: "localhost:6379".
        /// </summary>
        public static string Host { get; set; }

        /// <summary>
        /// Gets or sets Redis database number. Default: 0.
        /// </summary>
        public static int Db { get; set; }

        public static IRedisClientsManager PooledManager
        {
            get { return _pooledManager.Value; }
        }

        public static IRedisClientsManager BasicManager
        {
            get { return _basicManager.Value; }
        }
    }
}
