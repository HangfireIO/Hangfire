using ServiceStack.Redis;

namespace HangFire
{
    public class RedisFactory
    {
        private static long _db = 0;
        private static string _password = null;
        private static int _port = 6379;
        private static string _host = "localhost";

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
        public static string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// Gets or sets Redis database number. Default: 0
        /// </summary>
        public static long Db
        {
            get { return _db; }
            set { _db = value; }
        }

        public static IRedisClient Create()
        {
            return new RedisClient(
                Host, 
                Port,
                Password,
                Db);
        }
    }
}
