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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.States;

namespace Hangfire.Client
{
    public class BackgroundJobFactory : IBackgroundJobFactory
    {
        private readonly IJobFilterProvider _filterProvider;
        private readonly IBackgroundJobFactory _innerFactory;

        public BackgroundJobFactory()
            : this(JobFilterProviders.Providers)
        {
        }

        public BackgroundJobFactory([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, new CoreBackgroundJobFactory(new StateMachine(filterProvider)))
        {
        }

        public int RetryAttempts
        {
            get
            {
                if (_innerFactory is CoreBackgroundJobFactory factory)
                {
                    return factory.RetryAttempts;
                }

                return 0;
            }
            set
            {
                if (_innerFactory is CoreBackgroundJobFactory factory)
                {
                    factory.RetryAttempts = value;
                }
            }
        }

        internal BackgroundJobFactory(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IBackgroundJobFactory innerFactory)
        {
            if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
            if (innerFactory == null) throw new ArgumentNullException(nameof(innerFactory));

            _filterProvider = filterProvider;
            _innerFactory = innerFactory;
        }

        public IStateMachine StateMachine => _innerFactory.StateMachine;

        public BackgroundJob Create(CreateContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var filterInfo = GetFilters(context.Job);

            try
            {
                var createdContext = CreateWithFilters(context, filterInfo.ClientFilters);
                return createdContext.BackgroundJob;
            }
            catch (Exception ex)
            {
                var exceptionContext = new ClientExceptionContext(context, ex);

                InvokeExceptionFilters(exceptionContext, filterInfo.ClientExceptionFilters);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }

                return null;
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }

        private CreatedContext CreateWithFilters(
            CreateContext context, 
            IEnumerable<IClientFilter> filters)
        {
            var preContext = new CreatingContext(context);
            Func<CreatedContext> continuation = () =>
            {
                var backgroundJob = _innerFactory.Create(context);
                return new CreatedContext(context, backgroundJob, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokeClientFilter(filter, preContext, next));

            return thunk();
        }

        private static CreatedContext InvokeClientFilter(
            IClientFilter filter,
            CreatingContext preContext,
            Func<CreatedContext> continuation)
        {
            preContext.Profiler.InvokeMeasured(
                Tuple.Create(filter, preContext),
                InvokeOnCreating,
                $"OnCreating for {preContext.Job.Type.FullName}.{preContext.Job.Method.Name}");

            if (preContext.Canceled)
            {
                return new CreatedContext(preContext, null, true, null);
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
                postContext = new CreatedContext(preContext, null, false, ex);

                postContext.Profiler.InvokeMeasured(
                    Tuple.Create(filter, postContext),
                    InvokeOnCreated,
                    $"OnCreated for {postContext.BackgroundJob?.Id ?? "(null)"}");

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                postContext.Profiler.InvokeMeasured(
                    Tuple.Create(filter, postContext),
                    InvokeOnCreated,
                    $"OnCreated for {postContext.BackgroundJob?.Id ?? "(null)"}");
            }

            return postContext;
        }

        private static void InvokeOnCreating(Tuple<IClientFilter, CreatingContext> x)
        {
            try
            {
                x.Item1.OnCreating(x.Item2);
            }
            catch (Exception ex)
            {
                ex.PreserveOriginalStackTrace();
                throw;
            }
        }

        private static void InvokeOnCreated(Tuple<IClientFilter, CreatedContext> x)
        {
            try
            {
                x.Item1.OnCreated(x.Item2);
            }
            catch (Exception ex)
            {
                ex.PreserveOriginalStackTrace();
                throw;
            }
        }

        private static void InvokeExceptionFilters(
            ClientExceptionContext context, IEnumerable<IClientExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                context.Profiler.InvokeMeasured(
                    Tuple.Create(filter, context),
                    InvokeOnClientException,
                    $"OnClientException for {context.Job.Type.FullName}.{context.Job.Method.Name}");
            }
        }

        private static void InvokeOnClientException(Tuple<IClientExceptionFilter, ClientExceptionContext> x)
        {
            try
            {
                x.Item1.OnClientException(x.Item2);
            }
            catch (Exception ex)
            {
                ex.PreserveOriginalStackTrace();
                throw;
            }
        }
    }
}