using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HangFire
{
    internal class JobInvoker
    {
        private const string InvokeMethodName = "Perform";

        private readonly HangFireJobActivator _activator;
        private readonly IEnumerable<IServerFilter> _filters;

        public JobInvoker(HangFireJobActivator activator, IEnumerable<IServerFilter> filters)
        {
            if (activator == null)
            {
                throw new ArgumentNullException("activator");
            }
            if (filters == null)
            {
                throw new ArgumentNullException("filters");
            }

            _activator = activator;
            _filters = filters;
        }

        public void InvokeJob(string jobId, Dictionary<string, string> job)
        {
            object jobInstance = null;
            try
            {
                try
                {
                    var jobType = Type.GetType(job["Type"], true, true);
                    jobInstance = _activator.ActivateJob(jobType);
                }
                catch (Exception ex)
                {
                    throw new JobActivationException(
                        String.Format(
                            "An exception occured while trying to activate a job with the type '{0}'", 
                            job["Type"]),
                        ex);
                }

                var jobArguments = new Dictionary<string, string>(
                    JsonHelper.Deserialize<Dictionary<string, string>>(job["Args"]),
                    StringComparer.InvariantCultureIgnoreCase);

                var methodInfo = jobInstance.GetType().GetMethod(InvokeMethodName);
                if (methodInfo == null)
                {
                    throw new MissingMethodException(jobInstance.GetType().Name, InvokeMethodName);
                }

                var parametersInfo = methodInfo.GetParameters();
                var arguments = parametersInfo.Length > 0 ? new object[parametersInfo.Length] : null;

                var missingArguments = new List<String>();

                for (int i = 0; i < parametersInfo.Length; i++)
                {
                    var parameter = parametersInfo[i];
                    object value;
                    if (jobArguments.ContainsKey(parameter.Name))
                    {
                        var converter = TypeDescriptor.GetConverter(parameter.ParameterType);

                        // TODO: handle deserialization exception and display it in a friendly way.
                        value = converter.ConvertFromInvariantString(jobArguments[parameter.Name]);
                    }
                    else
                    {
                        if (parameter.IsOptional)
                        {
                            value = parameter.DefaultValue;
                        }
                        else
                        {
                            value = null;
                            missingArguments.Add(parameter.Name);
                        }
                    }

                    if (missingArguments.Count != 0)
                    {
                        throw new ArgumentException(
                            String.Format(
                                "Values for the following required arguments were not provided: {0}.",
                                String.Join(", ", missingArguments.Select(x => "'" + x + "'"))));
                    }

                    // ReSharper disable once PossibleNullReferenceException
                    arguments[i] = value;
                }

                Action performAction = () =>
                {
                    try
                    {
                        methodInfo.Invoke(jobInstance, arguments);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.GetBaseException();
                    }
                };

                InvokeFilters(jobId, job, jobInstance, performAction);
            }
            finally
            {
                var disposable = jobInstance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private void InvokeFilters(
            string jobId,
            Dictionary<string, string> job,
            object jobInstance,
            Action performAction)
        {
            var commandAction = performAction;

            var entries = _filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var currentEntry = entry;

                var filterContext = new ServerFilterContext(jobId, job, jobInstance, performAction);
                commandAction = () => currentEntry.ServerFilter(filterContext);
            }

            commandAction();
        }
    }
}
