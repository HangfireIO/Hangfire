using System;
using System.Collections.Generic;
using System.ComponentModel;
using HangFire.Client;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class OldFormatServerJobDescriptor : ServerJobDescriptorBase
    {
        private readonly JobActivator _activator;
        private readonly Dictionary<string, string> _arguments;

        internal OldFormatServerJobDescriptor(
            IRedisClient redis,
            JobActivator activator,
            string jobId,
            JobInvocationData data,
            Dictionary<string, string> arguments)
            : base(redis, jobId, data)
        {
            if (activator == null) throw new ArgumentNullException("activator");
            if (data == null) throw new ArgumentNullException("data");
            if (arguments == null) throw new ArgumentNullException("arguments");

            _activator = activator;
            _arguments = arguments;
        }

        internal override void Perform()
        {
            BackgroundJob instance = null;

            try
            {
                instance = (BackgroundJob)_activator.ActivateJob(InvocationData.Type);

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        String.Format("JobActivator returned NULL instance of the '{0}' type.", InvocationData.Type));
                }

                InitializeProperties(instance, _arguments);
                instance.Perform();
            }
            finally
            {
                var disposable = instance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private void InitializeProperties(BackgroundJob instance, Dictionary<string, string> arguments)
        {
            foreach (var arg in arguments)
            {
                var propertyInfo = InvocationData.Type.GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    try
                    {
                        var value = converter.ConvertFromInvariantString(arg.Value);
                        propertyInfo.SetValue(instance, value, null);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Could not set the property '{0}' of the instance of class '{1}'. See the inner exception for details.",
                                propertyInfo.Name,
                                InvocationData.Type),
                            ex);
                    }
                }
            }
        }
    }
}