using System;
using HangFire.Server;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis.DataTypes
{
    internal class RedisJob : IPersistentJob
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisJob(IRedisClient redis)
        {
            _redis = redis;
        }

        public StateAndInvocationData GetStateAndInvocationData(string id)
        {
            var jobData = _redis.GetAllEntriesFromHash(
                String.Format("hangfire:job:{0}", id));

            if (jobData.Count == 0) return null;

            var invocationData = new InvocationData();
            if (jobData.ContainsKey("Type"))
            {
                invocationData.Type = jobData["Type"];
            }
            if (jobData.ContainsKey("Method"))
            {
                invocationData.Method = jobData["Method"];
            }
            if (jobData.ContainsKey("ParameterTypes"))
            {
                invocationData.ParameterTypes = jobData["ParameterTypes"];
            }

            return new StateAndInvocationData
            {
                InvocationData = invocationData,
                State = jobData.ContainsKey("State") ? jobData["State"] : null,
            };
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
            RedisConnection.RemoveFromDequeuedList(_redis, payload.Queue, payload.Id);
        }
    }
}