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

namespace Hangfire.Server
{
    public class JobPerformanceProcess : IJobPerformanceProcess
    {
        private readonly IJobFilterProvider _filterProvider;
        private readonly IJobPerformanceProcess _innerProcess;

        public JobPerformanceProcess()
            : this(JobFilterProviders.Providers)
        {
        }

        public JobPerformanceProcess([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, new CoreJobPerformanceProcess())
        {
        }

        internal JobPerformanceProcess(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IJobPerformanceProcess innerProcess)
        {
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");
            if (innerProcess == null) throw new ArgumentNullException("innerProcess");

            _filterProvider = filterProvider;
            _innerProcess = innerProcess;
        }

        public object Run(PerformContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var filterInfo = GetFilters(context.BackgroundJob.Job);

            try
            {
                return PerformJobWithFilters(context, filterInfo.ServerFilters);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
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
                result = _innerProcess.Run(context);
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
                filter.OnPerforming(preContext);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception filterException)
            {
                throw new JobPerformanceException(
                    "An exception occurred during execution of one of the filters",
                    filterException);
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
                    filter.OnPerformed(postContext);
                }
                catch (Exception filterException)
                {
                    throw new JobPerformanceException(
                        "An exception occurred during execution of one of the filters",
                        filterException);
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
                    filter.OnPerformed(postContext);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception filterException)
                {
                    throw new JobPerformanceException(
                        "An exception occurred during execution of one of the filters",
                        filterException);
                }
            }

            return postContext;
        }

        private static void InvokeServerExceptionFilters(
            ServerExceptionContext context,
            IEnumerable<IServerExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                filter.OnServerException(context);
            }
        }
    }
}
