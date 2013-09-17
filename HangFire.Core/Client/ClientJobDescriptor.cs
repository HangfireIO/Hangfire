using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HangFire.Client
{
    public class ClientJobDescriptor
    {
        public ClientJobDescriptor(string jobId, Dictionary<string, string> job)
        {
            Job = job;
            JobId = jobId;
        }

        public string JobId { get; set; }

        internal Action EnqueueAction { get; set; }
        internal Dictionary<string, string> Job { get; private set; }  

        public void Enqueue()
        {
            EnqueueAction();
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

        internal IDictionary<string, string> SerializeProperties(object jobProperties)
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
