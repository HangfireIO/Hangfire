// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.Common.Filters;
using HangFire.Server.Filters;

namespace HangFire.Server.Performing
{
    internal class JobPerformanceProcess
    {
        private readonly Func<MethodData, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public JobPerformanceProcess()
        {
        }

        internal JobPerformanceProcess(IEnumerable<object> filters)
            : this()
        {
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            }
        }

        protected virtual JobFilterInfo GetFilters(MethodData methodData)
        {
            return new JobFilterInfo(_getFiltersThunk(methodData));
        }

        public void Run(PerformContext context, IJobPerformStrategy strategy)
        {
            var filterInfo = GetFilters(context.MethodData);

            try
            {
                PerformJobWithFilters(context, strategy, filterInfo.ServerFilters);
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
        }

        private static void PerformJobWithFilters(
            PerformContext context,
            IJobPerformStrategy strategy,
            IEnumerable<IServerFilter> filters)
        {
            var preContext = new PerformingContext(context);
            Func<PerformedContext> continuation = () =>
            {
                strategy.Perform();
                return new PerformedContext(context, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokePerformFilter(filter, preContext, next));
            
            thunk();
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
            catch (Exception filterException)
            {
                throw new JobPerformanceException(
                    "An exception occurred during execution of one of the filters",
                    filterException);
            }
            
            if (preContext.Canceled)
            {
                return new PerformedContext(
                    preContext, true, null);
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
                    preContext, false, ex);

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
                filter.OnPerformed(postContext);
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
