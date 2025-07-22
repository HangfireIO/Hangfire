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
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Profiling;

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Server
{
    public class BackgroundJobPerformer : IBackgroundJobPerformer
    {
        internal static readonly string ContextCanceledKey = "X_HF_Canceled";

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
            [CanBeNull] TaskScheduler? taskScheduler)
            : this(filterProvider, new CoreBackgroundJobPerformer(activator, taskScheduler))
        {
        }

        internal BackgroundJobPerformer(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IBackgroundJobPerformer innerPerformer)
        {
            _filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            _innerPerformer = innerPerformer ?? throw new ArgumentNullException(nameof(innerPerformer));
        }

        public object? Perform(PerformContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var filterInfo = GetFilters(context.BackgroundJob.Job);

            try
            {
                context.Performer = this;
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
                InvokeServerExceptionFilters(exceptionContext, filterInfo.ServerExceptionFiltersReversed);

                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
            finally
            {
                context.Performer = null;
            }

            return null;
        }

        private JobFilterInfo GetFilters(Job? job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }

        private object? PerformJobWithFilters(PerformContext context, JobFilterInfo.FilterCollection<IServerFilter> filters)
        {
            var preContext = new PerformingContext(context);
            var enumerator = filters.GetEnumerator();

            return InvokeNextServerFilter(ref enumerator, _innerPerformer, context, preContext).Result;
        }

        private static PerformedContext InvokeNextServerFilter(
            ref JobFilterInfo.FilterCollection<IServerFilter>.Enumerator enumerator,
            IBackgroundJobPerformer innerPerformer,
            PerformContext context,
            PerformingContext preContext)
        {
            if (enumerator.MoveNext())
            {
                return InvokeServerFilter(ref enumerator, innerPerformer, context, preContext);
            }

            var result = innerPerformer.Perform(context);
            return new PerformedContext(context, result, false, null);
        }

        private static PerformedContext InvokeServerFilter(
            ref JobFilterInfo.FilterCollection<IServerFilter>.Enumerator enumerator,
            IBackgroundJobPerformer innerPerformer,
            PerformContext context,
            PerformingContext preContext)
        {
            var filter = enumerator.Current;

            preContext.Profiler.InvokeMeasured(
                new KeyValuePair<IServerFilter, PerformingContext>(filter, preContext),
                InvokeOnPerforming,
                static ctx => $"OnPerforming for {ctx.Value.BackgroundJob.Id}" );
            
            if (preContext.Canceled)
            {
                if (!preContext.Items.ContainsKey(ContextCanceledKey))
                {
                    preContext.Items.Add(ContextCanceledKey, filter.GetType().Name);
                }
                
                return new PerformedContext(preContext, null, true, null);
            }

            var wasError = false;
            PerformedContext postContext;
            try
            {
                postContext = InvokeNextServerFilter(ref enumerator, innerPerformer, context, preContext);
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                wasError = true;
                postContext = new PerformedContext(
                    preContext, null, false, ex);

                postContext.Profiler.InvokeMeasured(
                    new KeyValuePair<IServerFilter, PerformedContext>(filter, postContext),
                    InvokeOnPerformed,
                    static ctx => $"OnPerformed for {ctx.Value.BackgroundJob.Id}");

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                postContext.Profiler.InvokeMeasured(
                    new KeyValuePair<IServerFilter, PerformedContext>(filter, postContext),
                    InvokeOnPerformed,
                    static ctx => $"OnPerformed for {ctx.Value.BackgroundJob.Id}");
            }

            return postContext;
        }

        private static void InvokeOnPerforming(KeyValuePair<IServerFilter, PerformingContext> ctx)
        {
            try
            {
                ctx.Key.OnPerforming(ctx.Value);
            }
            catch (Exception filterException) when (filterException.IsCatchableExceptionType())
            {
                CoreBackgroundJobPerformer.HandleJobPerformanceException(
                    filterException,
                    ctx.Value.CancellationToken, ctx.Value.BackgroundJob);
            }
        }

        private static void InvokeOnPerformed(KeyValuePair<IServerFilter, PerformedContext> ctx)
        {
            try
            {
                ctx.Key.OnPerformed(ctx.Value);
            }
            catch (Exception filterException) when (filterException.IsCatchableExceptionType())
            {
                CoreBackgroundJobPerformer.HandleJobPerformanceException(
                    filterException,
                    ctx.Value.CancellationToken, ctx.Value.BackgroundJob);
            }
        }

        private static void InvokeServerExceptionFilters(
            ServerExceptionContext context,
            JobFilterInfo.ReversedFilterCollection<IServerExceptionFilter> filters)
        {
            foreach (var filter in filters)
            {
                context.Profiler.InvokeMeasured(
                    new KeyValuePair<IServerExceptionFilter, ServerExceptionContext>(filter, context),
                    InvokeOnServerException,
                    static ctx => $"OnServerException for {ctx.Value.BackgroundJob.Id}");
            }
        }

        private static void InvokeOnServerException(KeyValuePair<IServerExceptionFilter, ServerExceptionContext> ctx)
        {
            try
            {
                ctx.Key.OnServerException(ctx.Value);
            }
            catch (Exception filterException) when (filterException.IsCatchableExceptionType())
            {
                CoreBackgroundJobPerformer.HandleJobPerformanceException(
                    filterException,
                    ctx.Value.CancellationToken, ctx.Value.BackgroundJob);
            }
        }
    }
}
