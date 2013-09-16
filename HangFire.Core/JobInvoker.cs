using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    internal class JobInvoker
    {
        public static readonly JobInvoker Current
            = new JobInvoker(
                HangFireConfiguration.Current.ServerFilters,
                HangFireConfiguration.Current.ClientFilters,
                Enumerable.Empty<IJobExceptionFilter>()); // TODO: replace with real value.

        static JobInvoker()
        {
        }

        private readonly IEnumerable<IServerJobFilter> _serverFilters;
        private readonly IEnumerable<IClientJobFilter> _clientFilters;
        private readonly IEnumerable<IJobExceptionFilter> _exceptionFilters;

        public JobInvoker(
            IEnumerable<IServerJobFilter> serverFilters, 
            IEnumerable<IClientJobFilter> clientFilters, 
            IEnumerable<IJobExceptionFilter> exceptionFilters)
        {
            if (serverFilters == null) throw new ArgumentNullException("serverFilters");
            if (exceptionFilters == null) throw new ArgumentNullException("exceptionFilters");

            _serverFilters = serverFilters;
            _clientFilters = clientFilters;
            _exceptionFilters = exceptionFilters;
        }

        public void PerformJob(WorkerContext workerContext, ServerJobDescriptor jobDescriptor)
        {
            try
            {
                PerformJobWithFilters(workerContext, jobDescriptor, _serverFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = InvokeServerExceptionFilters(workerContext, _exceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
        }

        public void EnqueueJob(ClientContext clientContext, ClientJobDescriptor jobDescriptor)
        {
            try
            {
                EnqueueWithFilters(clientContext, jobDescriptor, _clientFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = InvokeClientExceptionFilters(clientContext, _exceptionFilters, ex);
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
                    preContext.WorkerContext, preContext.JobDescriptor, true, null);
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
                    preContext.WorkerContext, preContext.JobDescriptor, false, ex);
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

        private static JobEnqueuedContext EnqueueWithFilters(
            ClientContext clientContext, 
            ClientJobDescriptor jobDescriptor, 
            IEnumerable<IClientJobFilter> filters)
        {
            var preContext = new JobEnqueueingContext(clientContext, jobDescriptor);
            Func<JobEnqueuedContext> continuation = () =>
            {
                jobDescriptor.Enqueue();
                return new JobEnqueuedContext(clientContext, jobDescriptor, false, null);
            };

            Func<JobEnqueuedContext> thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => EnqueueJobFilter(filter, preContext, next));

            return thunk();
        }

        private static JobEnqueuedContext EnqueueJobFilter(
            IClientJobFilter filter, 
            JobEnqueueingContext preContext, 
            Func<JobEnqueuedContext> continuation)
        {
            filter.OnJobEnqueueing(preContext);
            if (preContext.Canceled)
            {
                return new JobEnqueuedContext(
                    preContext.ClientContext, preContext.JobDescriptor, true, null);
            }

            var wasError = false;
            JobEnqueuedContext postContext;
            try
            {
                postContext = continuation();
            }
            catch (Exception ex)
            {
                wasError = true;
                postContext = new JobEnqueuedContext(
                    preContext.ClientContext, preContext.JobDescriptor, false, ex);

                filter.OnJobEnqueued(postContext);

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                filter.OnJobEnqueued(postContext);
            }

            return postContext;
        }

        private static ServerJobExceptionContext InvokeServerExceptionFilters(
            WorkerContext workerContext, IEnumerable<IJobExceptionFilter> filters, Exception exception)
        {
            var context = new ServerJobExceptionContext(workerContext, exception);
            foreach (var filter in filters.Reverse())
            {
                filter.OnServerException(context);
            }

            return context;
        }

        private static ClientJobExceptionContext InvokeClientExceptionFilters(
            ClientContext clientContext, IEnumerable<IJobExceptionFilter> filters, Exception exception)
        {
            var context = new ClientJobExceptionContext(clientContext, exception);
            foreach (var filter in filters.Reverse())
            {
                filter.OnClientException(context);
            }

            return context;
        }
    }
}
