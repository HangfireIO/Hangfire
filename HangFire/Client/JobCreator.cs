using System;
using System.Collections.Generic;
using System.Linq;

using HangFire.Filters;

namespace HangFire.Client
{
    internal class JobCreator
    {
        public static JobCreator Current { get; private set; }

        static JobCreator()
        {
            Current = new JobCreator();
        }

        private readonly Func<JobDescriptor, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public JobCreator()
        {
        }

        internal JobCreator(IEnumerable<object> filters)
            : this()
        {
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Invoke, null));
            }
        }

        protected virtual JobFilterInfo GetFilters(JobDescriptor descriptor)
        {
            return new JobFilterInfo(_getFiltersThunk(descriptor));
        }

        public void CreateJob(CreateContext context)
        {
            var filterInfo = GetFilters(context.JobDescriptor);

            try
            {
                CreateWithFilters(context, context.JobDescriptor, filterInfo.ClientFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = new ClientExceptionContext(context, ex);

                InvokeExceptionFilters(exceptionContext, filterInfo.ClientExceptionFilters);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
        }

        private static void CreateWithFilters(
            CreateContext createContext,
            ClientJobDescriptor jobDescriptor,
            IEnumerable<IClientFilter> filters)
        {
            var preContext = new CreatingContext(createContext);
            Func<CreatedContext> continuation = () =>
            {
                jobDescriptor.Create();
                return new CreatedContext(createContext, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokeClientFilter(filter, preContext, next));

            thunk();
        }

        private static CreatedContext InvokeClientFilter(
            IClientFilter filter,
            CreatingContext preContext,
            Func<CreatedContext> continuation)
        {
            filter.OnCreating(preContext);
            if (preContext.Canceled)
            {
                return new CreatedContext(
                    preContext, true, null);
            }

            var wasError = false;
            CreatedContext postContext;
            try
            {
                postContext = continuation();
            }
            catch (Exception ex)
            {
                wasError = true;
                postContext = new CreatedContext(
                    preContext, false, ex);

                filter.OnCreated(postContext);

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                filter.OnCreated(postContext);
            }

            return postContext;
        }

        private static void InvokeExceptionFilters(
            ClientExceptionContext context, IEnumerable<IClientExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                filter.OnClientException(context);
            }
        }
    }
}