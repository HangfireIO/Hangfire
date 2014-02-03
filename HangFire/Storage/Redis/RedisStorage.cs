using System;
using System.Collections.Generic;
using HangFire.Common;
using ServiceStack.Redis;

namespace HangFire.Storage.Redis
{
    public class RedisStorage : IStoredJobs
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisStorage(IRedisClient redis)
        {
            _redis = redis;
        }

        public Dictionary<string, string> Get(string id)
        {
            var job = _redis.GetAllEntriesFromHash(
                String.Format("hangfire:job:{0}", id));

            return job.Count != 0 ? job : null;
        }

        public void SetParameter(string id, string name, string value)
        {
            _redis.SetEntryInHash(
                String.Format(Prefix + "job:{0}", id),
                name,
                JobHelper.ToJson(value));
        }

        public string GetParameter(string id, string name)
        {
            return _redis.GetValueFromHash(
                String.Format(Prefix + "job:{0}", id),
                name);
        }
    }
}