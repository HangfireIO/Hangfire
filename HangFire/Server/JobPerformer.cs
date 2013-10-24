using System;
using System.Collections.Generic;
using System.Linq;

using HangFire.Filters;

namespace HangFire.Server
{
    internal class JobPerformer
    {
        private readonly IEnumerable<IServerFilter> _serverFilters;
        private readonly IEnumerable<IServerExceptionFilter> _serverExceptionFilters;

        public JobPerformer()
            : this(
                GlobalJobFilters.Filters.OfType<IServerFilter>(),
                GlobalJobFilters.Filters.OfType<IServerExceptionFilter>())
        {
        }

        public JobPerformer(
            IEnumerable<IServerFilter> serverFilters, 
            IEnumerable<IServerExceptionFilter> serverExceptionFilters)
        {
            if (serverFilters == null) throw new ArgumentNullException("serverFilters");
            if (serverExceptionFilters == null) throw new ArgumentNullException("serverExceptionFilters");

            _serverFilters = serverFilters;
            _serverExceptionFilters = serverExceptionFilters;
        }

        public void PerformJob(PerformContext context)
        {
            try
            {
                PerformJobWithFilters(context, _serverFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = new ServerExceptionContext(context, ex);
                InvokeServerExceptionFilters(exceptionContext, _serverExceptionFilters);

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
            foreach (var filter in filters)
            {
                filter.OnServerException(context);
            }
        }
    }
}
