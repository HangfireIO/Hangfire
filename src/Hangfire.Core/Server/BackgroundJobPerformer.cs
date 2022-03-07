// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Profiling;

namespace Hangfire.Server
{
    public class BackgroundJobPerformer : IBackgroundJobPerformer
    {
        private readonly IJobFilterProvider _filterProvider;
        private readonly IBackgroundJobPerformer _innerPerformer;

        public BackgroundJobPerformer()
            : this(JobFilterProviders.Providers)
        {
        }

        public BackgroundJobPerformer([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, JobActivator.Current)
        {
        }

        public BackgroundJobPerformer(
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] JobActivator activator)
            : this(filterProvider, activator, TaskScheduler.Default)
        {
        }

        public BackgroundJobPerformer(
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] JobActivator activator,
            [CanBeNull] TaskScheduler taskScheduler)
            : this(filterProvider, new CoreBackgroundJobPerformer(activator, taskScheduler))
        {
        }

        internal BackgroundJobPerformer(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IBackgroundJobPerformer innerPerformer)
        {
            if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
            if (innerPerformer == null) throw new ArgumentNullException(nameof(innerPerformer));

            _filterProvider = filterProvider;
            _innerPerformer = innerPerformer;
        }

        public object Perform(PerformContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var filterInfo = GetFilters(context.BackgroundJob.Job);

            try
            {
                return PerformJobWithFilters(context, filterInfo.ServerFilters);
            }
            catch (JobAbortedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // TODO: Catch only JobPerformanceException, and pass InnerException to filters in 2.0.0.

                if (ex is OperationCanceledException && context.CancellationToken.ShutdownToken.IsCancellationRequested)
                {
                    throw;
                }

                var exceptionContext = new ServerExceptionContext(context, ex);
                InvokeServerExceptionFilters(exceptionContext, filterInfo.ServerExceptionFilters);

                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }

            return null;
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }

        private object PerformJobWithFilters(PerformContext context, IEnumerable<IServerFilter> filters)
        {
            object result = null;

            var preContext = new PerformingContext(context);
            Func<PerformedContext> continuation = () =>
            {
                result = _innerPerformer.Perform(context);
                return new PerformedContext(context, result, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokePerformFilter(filter, preContext, next));
            
            thunk();

            return result;
        }

        private static PerformedContext InvokePerformFilter(
            IServerFilter filter, 
            PerformingContext preContext,
            Func<PerformedContext> continuation)
        {
            try
            {
                preContext.Profiler.InvokeMeasured(
                    Tuple.Create(filter, preContext),
                    InvokeOnPerforming,
                    $"OnPerforming for {preContext.BackgroundJob.Id}");
            }
            catch (Exception filterException)
            {
                CoreBackgroundJobPerformer.HandleJobPerformanceException(
                    filterException,
                    preContext.CancellationToken, preContext.BackgroundJob);
                throw;
            }
            
            if (preContext.Canceled)
            {
                return new PerformedContext(
                    preContext, null, true, null);
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
                    preContext, null, false, ex);

                try
                {
                    postContext.Profiler.InvokeMeasured(
                        Tuple.Create(filter, postContext),
                        InvokeOnPerformed,
                        $"OnPerformed for {postContext.BackgroundJob.Id}");
                }
                catch (Exception filterException)
                {
                    CoreBackgroundJobPerformer.HandleJobPerformanceException(
                        filterException,
                        postContext.CancellationToken, postContext.BackgroundJob);

                    throw;
                }

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                try
                {
                    postContext.Profiler.InvokeMeasured(
                        Tuple.Create(filter, postContext),
                        InvokeOnPerformed,
                        $"OnPerformed for {postContext.BackgroundJob.Id}");
                }
                catch (Exception filterException)
                {
                    CoreBackgroundJobPerformer.HandleJobPerformanceException(
                        filterException,
                        postContext.CancellationToken, postContext.BackgroundJob);

                    throw;
                }
            }

            return postContext;
        }

        private static void InvokeOnPerforming(Tuple<IServerFilter, PerformingContext> x)
        {
            x.Item1.OnPerforming(x.Item2);
        }

        private static void InvokeOnPerformed(Tuple<IServerFilter, PerformedContext> x)
        {
            x.Item1.OnPerformed(x.Item2);
        }

        private static void InvokeServerExceptionFilters(
            ServerExceptionContext context,
            IEnumerable<IServerExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                context.Profiler.InvokeMeasured(
                    Tuple.Create(filter, context),
                    InvokeOnServerException,
                    $"OnServerException for {context.BackgroundJob.Id}");
            }
        }

        private static void InvokeOnServerException(Tuple<IServerExceptionFilter, ServerExceptionContext> x)
        {
            x.Item1.OnServerException(x.Item2);
        }
    }
}
