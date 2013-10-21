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
            Dictionary<string, string> jobParameters,
            JobState state)
        {
            _redis = redis;
            _stateMachine = new StateMachine(redis);

            _state = state;
            _jobParameters = jobParameters;
            JobId = jobId;
        }

        public string JobId { get; set; } 

        public void Create()
        {
            _redis.SetRangeInHash(
                String.Format("hangfire:job:{0}", JobId),
                _jobParameters);

            _stateMachine.ChangeState(JobId, _state);
        }

        public void SetParameter(string name, object value)
        {
            _jobParameters.Add(name, JobHelper.ToJson(value));
        }

        public T GetParameter<T>(string name)
        {
            return _jobParameters.ContainsKey(name)
                ? JobHelper.FromJson<T>(_jobParameters[name])
                : default(T);
        }

        
    }
}
