using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Server;
using Hangfire.Storage;

// ReSharper disable once CheckNamespace
namespace Hangfire.Common
{
    partial class Job
    {
        [Obsolete("Please use Job(Type, MethodInfo, object[]) ctor overload instead. Will be removed in 2.0.0.")]
        public Job([NotNull] Type type, [NotNull] MethodInfo method, [NotNull] string[] arguments)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));

            Validate(type, nameof(type), method, nameof(method), arguments.Length, nameof(arguments));

            Type = type;
            Method = method;
            Args = InvocationData.DeserializeArguments(method, arguments);
        }

        /// <exclude />
        [NotNull]
        [Obsolete("Please use `Args` property instead to avoid unnecessary serializations/deserializations. Will be deleted in 2.0.0.")]
        public string[] Arguments => InvocationData.SerializeArguments(Args);

        /// <exclude />
        [Obsolete("This method is deprecated. Please use `CoreBackgroundJobPerformer` or `BackgroundJobPerformer` classes instead. Will be removed in 2.0.0.")]
        public object Perform(JobActivator activator, IJobCancellationToken cancellationToken)
        {
            if (activator == null) throw new ArgumentNullException(nameof(activator));
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));

            object instance = null;

            object result;
            try
            {
                if (!Method.IsStatic)
                {
                    instance = Activate(activator);
                }

                var deserializedArguments = DeserializeArguments(cancellationToken);
                result = InvokeMethod(instance, deserializedArguments, cancellationToken.ShutdownToken);
            }
            finally
            {
                Dispose(instance);
            }

            return result;
        }

        [Obsolete("Will be removed in 2.0.0")]
        private object Activate(JobActivator activator)
        {
            try
            {
                var instance = activator.ActivateJob(Type);

                if (instance == null)
                {
                    throw new InvalidOperationException($"JobActivator returned NULL instance of the '{Type}' type.");
                }

                return instance;
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "An exception occurred during job activation.",
                    ex);
            }
        }

        [Obsolete("Will be removed in 2.0.0")]
        private object[] DeserializeArguments(IJobCancellationToken cancellationToken)
        {
            try
            {
                var parameters = Method.GetParameters();
                var result = new List<object>(Arguments.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var argument = Arguments[i];

                    object value;

                    if (typeof(IJobCancellationToken).GetTypeInfo().IsAssignableFrom(parameter.ParameterType.GetTypeInfo()))
                    {
                        value = cancellationToken;
                    }
                    else
                    {
                        try
                        {
                            value = argument != null
                                ? SerializationHelper.Deserialize(argument, parameter.ParameterType, SerializationOption.User)
                                : null;
                        }
                        catch (Exception)
                        {
                            if (parameter.ParameterType == typeof(object))
                            {
                                // Special case for handling object types, because string can not
                                // be converted to object type.
                                value = argument;
                            }
                            else
                            {
#if NETFULL
                                var converter = TypeDescriptor.GetConverter(parameter.ParameterType);
                                value = converter.ConvertFromInvariantString(argument);
#else
                                DateTime dateTime;
                                if (parameter.ParameterType == typeof(DateTime) && InvocationData.ParseDateTimeArgument(argument, out dateTime))
                                {
                                    value = dateTime;
                                }
                                else
                                {
                                    throw;
                                }
#endif
                            }
                        }
                    }

                    result.Add(value);
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "An exception occurred during arguments deserialization.",
                    ex);
            }
        }

        [Obsolete("Will be removed in 2.0.0")]
        private object InvokeMethod(object instance, object[] deserializedArguments, CancellationToken shutdownToken)
        {
            try
            {
                return Method.Invoke(instance, deserializedArguments);
            }
            catch (TargetInvocationException ex)
            {
                CoreBackgroundJobPerformer.HandleJobPerformanceException(ex.InnerException, shutdownToken);
                throw;
            }
        }

        [Obsolete("Will be removed in 2.0.0")]
        private static void Dispose(object instance)
        {
            try
            {
                var disposable = instance as IDisposable;
                disposable?.Dispose();
            }
            catch (Exception ex)
            {
                throw new JobPerformanceException(
                    "Job has been performed, but an exception occurred during disposal.",
                    ex);
            }
        }
    }
}