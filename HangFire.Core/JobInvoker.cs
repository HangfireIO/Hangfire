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

        public void InvokeJob(JobDescription jobDescription)
        {
            if (jobDescription == null)
            {
                throw new ArgumentNullException("jobDescription");
            }

            object job = null;
            try
            {
                try
                {
                    var jobType = Type.GetType(jobDescription.JobType, true, true);
                    job = _activator.ActivateJob(jobType);
                }
                catch (Exception ex)
                {
                    throw new JobActivationException(
                        String.Format(
                            "An exception occured while trying to activate a job with the type '{0}'", 
                            jobDescription.JobType),
                        ex);
                }

                var jobArguments = new Dictionary<string, string>(
                    jobDescription.Args,
                    StringComparer.InvariantCultureIgnoreCase);

                var methodInfo = job.GetType().GetMethod(InvokeMethodName);
                if (methodInfo == null)
                {
                    throw new MissingMethodException(job.GetType().Name, InvokeMethodName);
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
                var disposable = job as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
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
