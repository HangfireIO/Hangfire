using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    internal class JobInvoker
    {
        private readonly HangFireJobActivator _jobActivator;
        private readonly IEnumerable<IServerJobFilter> _filters;
        private readonly IEnumerable<IJobExceptionFilter> _exceptionFilters;

        public JobInvoker(
            HangFireJobActivator jobActivator,
            IEnumerable<IServerJobFilter> filters, 
            IEnumerable<IJobExceptionFilter> exceptionFilters)
        {
            if (filters == null) throw new ArgumentNullException("filters");
            if (exceptionFilters == null) throw new ArgumentNullException("exceptionFilters");

            _jobActivator = jobActivator;
            _filters = filters;
            _exceptionFilters = exceptionFilters;
        }

        public void PerformJob(WorkerContext workerContext)
        {
            HangFireJob jobInstance = null;

            try
            {
                var type = Type.GetType(workerContext.JobType, true, true);
                jobInstance = _jobActivator.ActivateJob(type);
                jobInstance.Initialize(workerContext);

                PerformJobWithFilters(workerContext, jobInstance, _filters);
            }
            catch (Exception ex)
            {
                var exceptionContext = InvokeExceptionFilters(workerContext, _exceptionFilters, ex);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
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

        private static JobPerformedContext PerformJobWithFilters(
            WorkerContext workerContext, HangFireJob jobInstance, IEnumerable<IServerJobFilter> filters)
        {
            var preContext = new JobPerformingContext(workerContext, jobInstance);
            Func<JobPerformedContext> continuation = () =>
            {
                jobInstance.Perform();
                return new JobPerformedContext(workerContext, jobInstance, false, null);
            };

            Func<JobPerformedContext> thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => PerformJobFilter(filter, preContext, next));

            return thunk();
        }

        private static JobPerformedContext PerformJobFilter(
            IServerJobFilter filter, JobPerformingContext preContext, Func<JobPerformedContext> continuation)
        {
            filter.OnJobPerforming(preContext);
            if (preContext.Canceled)
            {
                return new JobPerformedContext(
                    preContext.WorkerContext, preContext.JobInstance, true, null);
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
                    preContext.WorkerContext, preContext.JobInstance, false, ex);
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

        private static JobExceptionContext InvokeExceptionFilters(
            WorkerContext workerContext, IEnumerable<IJobExceptionFilter> filters, Exception exception)
        {
            var context = new JobExceptionContext(workerContext, exception);
            foreach (var filter in filters.Reverse())
            {
                filter.OnException(context);
            }

            return context;
        }
    }
}
