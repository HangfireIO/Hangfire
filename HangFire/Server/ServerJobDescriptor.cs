using System;
using System.Collections.Generic;
using System.ComponentModel;
using ServiceStack.Redis;

namespace HangFire.Server
{
    public class ServerJobDescriptor : IDisposable
    {
        private readonly IRedisClient _redis;
        private readonly BackgroundJob _jobInstance;

        internal ServerJobDescriptor(
            IRedisClient redis,
            JobActivator activator,
            JobPayload payload)
        {
            _redis = redis;
            if (activator == null) throw new ArgumentNullException("activator");
            if (payload == null) throw new ArgumentNullException("payload");

            JobId = payload.Id;

            Type = Type.GetType(payload.Type, true, true);
            _jobInstance = activator.ActivateJob(Type);

            if (_jobInstance == null)
            {
                throw new InvalidOperationException(String.Format(
                    "{0} returned NULL instance of the '{1}' type.",
                    activator.GetType().FullName,
                    Type.FullName));
            }

            var args = JobHelper.FromJson<Dictionary<string, string>>(payload.Args);

            foreach (var arg in args)
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

        public Type Type { get; private set; }

        public void SetParameter(string name, object value)
        {
            _redis.SetEntryInHash(
                String.Format("hangfire:job:{0}", JobId),
                name,
                JobHelper.ToJson(value));
        }

        public T GetParameter<T>(string name)
        {
            var value = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobId),
                name);

            return JobHelper.FromJson<T>(value);
        }

        internal void Perform()
        {
            _jobInstance.Perform();
        }

        internal void Dispose()
        {
            var disposable = _jobInstance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}
