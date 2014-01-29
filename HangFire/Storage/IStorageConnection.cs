using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using HangFire.Common;
using ServiceStack.Redis;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IAtomicWriteTransaction CreateWriteTransaction();

        IDisposable AcquireLock(string resource, TimeSpan timeOut);

        IStoredJobs Jobs { get; }
    }

    public interface IStoredJobs
    {
        Dictionary<string, string> Get(string id);
        void SetParameter(string id, string name, string value);
        string GetParameter(string id, string name);
    }

    public class RedisStorageConnection : IStorageConnection
    {
        private const string Prefix = "hangfire:";
        private readonly IRedisClient _redis;

        public RedisStorageConnection(IRedisClient redis)
        {
            _redis = redis;
            var storage = new RedisStorage(redis);
            Jobs = storage;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public IAtomicWriteTransaction CreateWriteTransaction()
        {
            return new RedisAtomicWriteTransaction(_redis.CreateTransaction());
        }

        public IDisposable AcquireLock(string resource, TimeSpan timeOut)
        {
            return _redis.AcquireLock(Prefix + resource, timeOut);
        }

        public IStoredJobs Jobs { get; private set; }
    }

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