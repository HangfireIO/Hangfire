using System;
using System.Collections.Generic;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    public class ClientJobDescriptor
    {
        private readonly IRedisClient _redis;
        private readonly StateMachine _stateMachine;

        private readonly JobState _state;
        private readonly IDictionary<string, string> _jobParameters; 

        internal ClientJobDescriptor(
            IRedisClient redis,
            string jobId, 
            IDictionary<string, string> jobParameters,
            JobState state)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (jobParameters == null) throw new ArgumentNullException("jobParameters");
            if (state == null) throw new ArgumentNullException("state");

            _redis = redis;
            _stateMachine = new StateMachine(redis);

            _state = state;
            _jobParameters = jobParameters;
            JobId = jobId;
        }

        public string JobId { get; set; } 

        public void SetParameter(string name, object value)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            // TODO: this method could be called from the Created method of a filter.
            //       In this case we'll lose the assigning value.

            _jobParameters.Add(name, JobHelper.ToJson(value));
        }

        public T GetParameter<T>(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            return _jobParameters.ContainsKey(name)
                ? JobHelper.FromJson<T>(_jobParameters[name])
                : default(T);
        }

        internal void Create()
        {
            _redis.SetRangeInHash(
                String.Format("hangfire:job:{0}", JobId),
                _jobParameters);

            // TODO: creation is non-atomic. If the following line will not be called,
            //       the job's key will stay for a long time in Redis.

            _stateMachine.ChangeState(JobId, _state);
        }
    }
}
