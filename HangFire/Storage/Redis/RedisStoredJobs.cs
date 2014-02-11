using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Server;
using ServiceStack.Redis;

namespace HangFire.Storage.Redis
{
    public class RedisStoredJobs : IStoredJobs
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisStoredJobs(IRedisClient redis)
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
                value);
        }

        public string GetParameter(string id, string name)
        {
            return _redis.GetValueFromHash(
                String.Format(Prefix + "job:{0}", id),
                name);
        }

        public void Complete(JobPayload payload)
        {
            RedisStorageConnection.RemoveFromDequeuedList(_redis, payload.Queue, payload.Id);
        }
    }
}