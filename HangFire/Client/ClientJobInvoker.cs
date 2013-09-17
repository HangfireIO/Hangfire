using System;
using System.Collections.Generic;
using System.Linq;

using HangFire.Filters;

namespace HangFire.Client
{
    internal class ClientJobInvoker
    {
        public static ClientJobInvoker Current { get; private set; }

        static ClientJobInvoker()
        {
            Current = new ClientJobInvoker(
                GlobalJobFilters.Filters.OfType<IClientJobFilter>(),
                GlobalJobFilters.Filters.OfType<IClientJobExceptionFilter>());
        }

        private readonly IEnumerable<IClientJobFilter> _clientFilters;
        private readonly IEnumerable<IClientJobExceptionFilter> _clientExceptionFilters;

        public ClientJobInvoker(
            IEnumerable<IClientJobFilter> clientFilters,
            IEnumerable<IClientJobExceptionFilter> clientExceptionFilters)
        {
            if (clientFilters == null) throw new ArgumentNullException("clientFilters");
            if (clientExceptionFilters == null) throw new ArgumentNullException("clientExceptionFilters");

            _clientFilters = clientFilters;
            _clientExceptionFilters = clientExceptionFilters;
        }

        public void EnqueueJob(ClientContext clientContext, ClientJobDescriptor jobDescriptor)
        {
            try
            {
                EnqueueWithFilters(clientContext, jobDescriptor, _clientFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = InvokeClientExceptionFilters(clientContext, _clientExceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
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

        private static ClientJobExceptionContext InvokeClientExceptionFilters(
            ClientContext clientContext, IEnumerable<IClientJobExceptionFilter> filters, Exception exception)
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