// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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

        internal BackgroundJobFactory(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IBackgroundJobFactory innerFactory)
        {
            if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
            if (innerFactory == null) throw new ArgumentNullException(nameof(innerFactory));

            _filterProvider = filterProvider;
            _innerFactory = innerFactory;
        }

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
            filter.OnCreating(preContext);
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

                filter.OnCreated(postContext);

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                filter.OnCreated(postContext);
            }

            return postContext;
        }

        private static void InvokeExceptionFilters(
            ClientExceptionContext context, IEnumerable<IClientExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                filter.OnClientException(context);
            }
        }
    }
}