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
using HangFire.Client.Filters;
using HangFire.Common;
using HangFire.Common.Filters;

namespace HangFire.Client
{
    internal class JobCreationProcess : IJobCreationProcess
    {
        public static JobCreationProcess Instance { get; private set; }

        static JobCreationProcess()
        {
            Instance = new JobCreationProcess();
        }

        private readonly Func<Job, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public JobCreationProcess()
        {
        }

        internal JobCreationProcess(IEnumerable<object> filters)
            : this()
        {
            if (filters == null) throw new ArgumentNullException("filters");

            _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
        }

        public void Run(CreateContext context, IJobCreator creator)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (creator == null) throw new ArgumentNullException("creator");

            var filterInfo = GetFilters(context.Job);

            try
            {
                CreateWithFilters(context, creator, filterInfo.ClientFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = new ClientExceptionContext(context, ex);

                InvokeExceptionFilters(exceptionContext, filterInfo.ClientExceptionFilters);
                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_getFiltersThunk(job));
        }

        private static void CreateWithFilters(
            CreateContext context,
            IJobCreator creator,
            IEnumerable<IClientFilter> filters)
        {
            var preContext = new CreatingContext(context);
            Func<CreatedContext> continuation = () =>
            {
                creator.Create();
                return new CreatedContext(context, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokeClientFilter(filter, preContext, next));

            thunk();
        }

        private static CreatedContext InvokeClientFilter(
            IClientFilter filter,
            CreatingContext preContext,
            Func<CreatedContext> continuation)
        {
            filter.OnCreating(preContext);
            if (preContext.Canceled)
            {
                return new CreatedContext(
                    preContext, true, null);
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
                postContext = new CreatedContext(
                    preContext, false, ex);

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