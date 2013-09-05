using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HangFire
{
    internal class JobInvoker
    {
        private readonly HangFireJobActivator _activator;
        private readonly IEnumerable<IServerFilter> _filters;

        public JobInvoker(HangFireJobActivator activator, IEnumerable<IServerFilter> filters)
        {
            _activator = activator;
            _filters = filters;
        }

        public void ProcessJob(JobDescription jobDescription)
        {
            object job = null;
            try
            {
                // TODO: what to do with type resolving exceptions?
                var jobType = Type.GetType(jobDescription.JobType);
                job = _activator.ActivateJob(jobType);

                var jobArguments = new Dictionary<string, string>(
                    jobDescription.Args,
                    StringComparer.InvariantCultureIgnoreCase);

                // TODO: throw friendly exception when no "Perform" method is defined.
                var methodInfo = job.GetType().GetMethod("Perform");
                var parametersInfo = methodInfo.GetParameters();

                var arguments = parametersInfo.Length > 0 ? new object[parametersInfo.Length] : null;

                // TODO: what to do with redundant or absent parameters?
                for (int i = 0; i < parametersInfo.Length; i++)
                {
                    var parameter = parametersInfo[i];
                    object value = parameter.DefaultValue;
                    if (jobArguments.ContainsKey(parameter.Name))
                    {
                        var converter = TypeDescriptor.GetConverter(parameter.ParameterType);

                        // TODO: handle deserialization exception and display it in a friendly way.
                        value = converter.ConvertFromInvariantString(jobArguments[parameter.Name]);
                    }

                    // ReSharper disable once PossibleNullReferenceException
                    arguments[i] = value;
                }

                Action performAction = () =>
                {
                    try
                    {
                        methodInfo.Invoke(job, arguments);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.GetBaseException();
                    }
                };

                InvokeFilters(job, jobDescription, performAction);
            }
            finally
            {
                if (job != null)
                {
                    var disposable = job as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        private void InvokeFilters(object job, JobDescription jobDescription, Action action)
        {
            var commandAction = action;

            var entries = _filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var currentEntry = entry;

                var filterContext = new ServerFilterContext(job, jobDescription, commandAction);
                commandAction = () => currentEntry.ServerFilter(filterContext);
            }

            commandAction();
        }
    }
}
