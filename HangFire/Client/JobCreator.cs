// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;

using HangFire.Filters;

namespace HangFire.Client
{
    internal class JobCreator
    {
        public static JobCreator Instance { get; private set; }

        static JobCreator()
        {
            Instance = new JobCreator();
        }

        private readonly Func<JobMethod, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public JobCreator()
        {
        }

        internal JobCreator(IEnumerable<object> filters)
            : this()
        {
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            }
        }

        protected virtual JobFilterInfo GetFilters(JobMethod method)
        {
            return new JobFilterInfo(_getFiltersThunk(method));
        }

        public void CreateJob(CreateContext context)
        {
            var filterInfo = GetFilters(context.JobDescriptor.JobMethod);

            try
            {
                CreateWithFilters(context, context.JobDescriptor, filterInfo.ClientFilters);
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

        private static void CreateWithFilters(
            CreateContext createContext,
            ClientJobDescriptor jobDescriptor,
            IEnumerable<IClientFilter> filters)
        {
            var preContext = new CreatingContext(createContext);
            Func<CreatedContext> continuation = () =>
            {
                jobDescriptor.Create();
                return new CreatedContext(createContext, false, null);
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