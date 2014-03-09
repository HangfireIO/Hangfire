using System.Linq;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    internal class RedisStoredSets : IStoredSets
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisStoredSets(IRedisClient redis)
        {
            _redis = redis;
        }

        public string GetFirstByLowestScore(string key, long fromScore, long toScore)
        {
            return _redis.GetRangeFromSortedSetByLowestScore(
                Prefix + key, fromScore, toScore, 0, 1)
                .FirstOrDefault();
        }
    }
}