using System;
using System.Collections.Generic;
using System.Linq;

using HangFire.Filters;

namespace HangFire.Server
{
    internal class JobPerformer
    {
        private readonly Func<JobDescriptor, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public JobPerformer()
        {
        }

        internal JobPerformer(IEnumerable<object> filters)
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

        public void PerformJob(PerformContext context)
        {
            var filterInfo = GetFilters(context.JobDescriptor);

            try
            {
                PerformJobWithFilters(context, filterInfo.ServerFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = new ServerExceptionContext(context, ex);
                InvokeServerExceptionFilters(exceptionContext, filterInfo.ServerExceptionFilters);

                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
        }

        private static void PerformJobWithFilters(
            PerformContext context,
            IEnumerable<IServerFilter> filters)
        {
            var preContext = new PerformingContext(context);
            Func<PerformedContext> continuation = () =>
            {
                context.JobDescriptor.Perform();
                return new PerformedContext(context, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokePerformFilter(filter, preContext, next));

            thunk();
        }

        private static PerformedContext InvokePerformFilter(
            IServerFilter filter, 
            PerformingContext preContext,
            Func<PerformedContext> continuation)
        {
            filter.OnPerforming(preContext);
            if (preContext.Canceled)
            {
                return new PerformedContext(
                    preContext, true, null);
            }

            var wasError = false;
            PerformedContext postContext;
            try
            {
                postContext = continuation();
            }
            catch (Exception ex)
            {
                wasError = true;
                postContext = new PerformedContext(
                    preContext, false, ex);
                filter.OnPerformed(postContext);

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                filter.OnPerformed(postContext);
            }

            return postContext;
        }

        private static void InvokeServerExceptionFilters(
            ServerExceptionContext context,
            IEnumerable<IServerExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                filter.OnServerException(context);
            }
        }
    }
}
