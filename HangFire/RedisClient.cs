using BookSleeve;

namespace HangFire
{
    internal static class RedisClient
    {
        public static RedisConnection CreateConnection()
        {
            var connection = new RedisConnection("localhost");
            connection.Open();

            return connection;
        }
    }
}
