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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
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
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                ex.PreserveOriginalStackTrace();
                throw;
            }
        }
    }
}