using System;
using HangFire.Client;
using ServiceStack.Redis;

namespace HangFire.Server
{
    public abstract class ServerJobDescriptorBase : JobDescriptor
    {
        private readonly IRedisClient _redis;

        protected ServerJobDescriptorBase(
            IRedisClient redis,
            string jobId, 
            JobInvocationData invocationData) 
            : base(jobId, invocationData)
        {
            if (redis == null) throw new ArgumentNullException("redis");

            _redis = redis;
        }

        public override void SetParameter(string name, object value)
        {
            _redis.SetEntryInHash(
                String.Format("hangfire:job:{0}", JobId),
                name,
                JobHelper.ToJson(value));
        }

        public override T GetParameter<T>(string name)
        {
            var value = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobId),
                name);

            return JobHelper.FromJson<T>(value);
        }

        internal abstract void Perform();
    }
}