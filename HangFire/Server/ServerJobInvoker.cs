using System;
using System.Collections.Generic;
using System.Linq;

using HangFire.Filters;

namespace HangFire.Server
{
    internal class ServerJobInvoker
    {
        static ServerJobInvoker()
        {
            Current = new ServerJobInvoker(
                GlobalJobFilters.Filters.OfType<IServerJobFilter>(),
                GlobalJobFilters.Filters.OfType<IServerJobExceptionFilter>());
        }

        public static ServerJobInvoker Current { get; private set; }

        private readonly IEnumerable<IServerJobFilter> _serverFilters;
        private readonly IEnumerable<IServerJobExceptionFilter> _serverExceptionFilters;

        public ServerJobInvoker(
            IEnumerable<IServerJobFilter> serverFilters, 
            IEnumerable<IServerJobExceptionFilter> serverExceptionFilters)
        {
            if (serverFilters == null) throw new ArgumentNullException("serverFilters");
            if (serverExceptionFilters == null) throw new ArgumentNullException("serverExceptionFilters");

            _serverFilters = serverFilters;
            _serverExceptionFilters = serverExceptionFilters;
        }

        public void PerformJob(WorkerContext workerContext, ServerJobDescriptor jobDescriptor)
        {
            try
            {
                PerformJobWithFilters(workerContext, jobDescriptor, _serverFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = InvokeServerExceptionFilters(workerContext, _serverExceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
        }

        private static JobPerformedContext PerformJobWithFilters(
            WorkerContext workerContext, 
            ServerJobDescriptor jobDescriptor, 
            IEnumerable<IServerJobFilter> filters)
        {
            var preContext = new JobPerformingContext(workerContext, jobDescriptor);
            Func<JobPerformedContext> continuation = () =>
            {
                jobDescriptor.Perform();
                return new JobPerformedContext(workerContext, jobDescriptor, false, null);
            };

            Func<JobPerformedContext> thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => PerformJobFilter(filter, preContext, next));

            return thunk();
        }

        private static JobPerformedContext PerformJobFilter(
            IServerJobFilter filter, 
            JobPerformingContext preContext,
            Func<JobPerformedContext> continuation)
        {
            filter.OnJobPerforming(preContext);
            if (preContext.Canceled)
            {
                return new JobPerformedContext(
                    preContext, preContext.JobDescriptor, true, null);
            }

            var wasError = false;
            JobPerformedContext postContext;
            try
            {
                postContext = continuation();
            }
            catch (Exception ex)
            {
                wasError = true;
                postContext = new JobPerformedContext(
                    preContext, preContext.JobDescriptor, false, ex);
                filter.OnJobPerformed(postContext);

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                filter.OnJobPerformed(postContext);
            }

            return postContext;
        }

        private static ServerJobExceptionContext InvokeServerExceptionFilters(
            WorkerContext workerContext, IEnumerable<IServerJobExceptionFilter> filters, Exception exception)
        {
            var context = new ServerJobExceptionContext(workerContext, exception);
            foreach (var filter in filters.Reverse())
            {
                filter.OnServerException(context);
            }

            return context;
        }
    }
}
