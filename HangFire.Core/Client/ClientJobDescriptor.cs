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
                        // TODO: handle conversion exception and display it in a friendly way.
                        var converter = TypeDescriptor.GetConverter(propertyValue.GetType());
                        value = converter.ConvertToInvariantString(propertyValue);
                    }

                    result.Add(descriptor.Name, value);
                }
            }

            return result;
        }
    }
}
