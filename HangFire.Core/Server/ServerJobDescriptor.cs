using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HangFire.Server
{
    public class ServerJobDescriptor : IDisposable
    {
        private readonly BackgroundJob _jobInstance;

        public ServerJobDescriptor(
            JobActivator activator,
            string jobId,
            string jobType, 
            IEnumerable<KeyValuePair<string, string>> jobProperties)
        {
            JobId = jobId;

            var type = Type.GetType(jobType, true, true);
            _jobInstance = activator.ActivateJob(type);

            // TODO: handle the null result

            foreach (var arg in jobProperties)
            {
                var propertyInfo = _jobInstance.GetType().GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    try
                    {
                        var value = converter.ConvertFromInvariantString(arg.Value);
                        propertyInfo.SetValue(_jobInstance, value, null);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Could not set the property '{0}' of the instance of class '{1}'. See the inner exception for details.",
                                propertyInfo.Name, _jobInstance.GetType().Name),
                            ex);
                    }
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
