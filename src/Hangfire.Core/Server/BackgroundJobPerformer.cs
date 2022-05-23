// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
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
            catch (Exception filterException) when (filterException.IsCatchableExceptionType())
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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
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
                catch (Exception filterException) when (filterException.IsCatchableExceptionType())
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
                catch (Exception filterException) when (filterException.IsCatchableExceptionType())
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
