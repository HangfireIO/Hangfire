using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HangFire
{
    public class ServerJobDescriptor : IDisposable
    {
        private readonly HangFireJob _jobInstance;

        public ServerJobDescriptor(
            HangFireJobActivator activator,
            string jobId,
            string jobType, 
            IEnumerable<KeyValuePair<string, string>> jobProperties)
        {
            JobId = jobId;

            var type = Type.GetType(jobType, true, true);
            _jobInstance = activator.ActivateJob(type);

            foreach (var arg in jobProperties)
            {
                var propertyInfo = _jobInstance.GetType().GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    // TODO: handle deserialization exception and display it in a friendly way.
                    var value = converter.ConvertFromInvariantString(arg.Value);
                    propertyInfo.SetValue(_jobInstance, value, null);
                }
            }
        }

        public string JobId { get; private set; }

        public void Perform()
        {
            _jobInstance.Perform();
        }

        public void SetParameter(string name, object value)
        {
            _jobInstance.SetParameter(name, value);
        }

        public T GetParameter<T>(string name)
        {
            return _jobInstance.GetParameter<T>(name);
        }

        public void Dispose()
        {
            var disposable = _jobInstance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
