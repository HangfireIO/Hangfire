using System;
using System.Collections.Generic;
using System.ComponentModel;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    public class ClientJobDescriptor
    {
        private readonly IRedisClient _redis;
        private readonly JobState _state;

        internal ClientJobDescriptor(
            IRedisClient redis,
            string jobId, Dictionary<string, string> job,
            JobState state)
        {
            _redis = redis;
            _state = state;
            Job = job;
            JobId = jobId;
        }

        public string JobId { get; set; }

        internal Dictionary<string, string> Job { get; private set; }  

        public void Enqueue()
        {
            _redis.SetRangeInHash(
                String.Format("hangfire:job:{0}", JobId),
                Job);

            JobState.Apply(_redis, JobId, _state);
        }

        public void SetParameter(string name, object value)
        {
            Job.Add(name, JobHelper.ToJson(value));
        }

        public T GetParameter<T>(string name)
        {
            return Job.ContainsKey(name)
                ? JobHelper.FromJson<T>(Job[name])
                : default(T);
        }

        internal static IDictionary<string, string> SerializeProperties(object jobProperties)
        {
            var result = new Dictionary<string, string>();
            if (jobProperties != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(jobProperties))
                {
                    var propertyValue = descriptor.GetValue(jobProperties);
                    string value = null;

                    if (propertyValue != null)
                    {
                        try
                        {
                            var converter = TypeDescriptor.GetConverter(propertyValue.GetType());
                            value = converter.ConvertToInvariantString(propertyValue);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                String.Format(
                                    "Could not convert property '{0}' of type '{1}' to a string. See the inner exception for details.",
                                    descriptor.Name,
                                    descriptor.PropertyType),
                                ex);
                        }
                    }

                    result.Add(descriptor.Name, value);
                }
            }

            return result;
        }
    }
}
