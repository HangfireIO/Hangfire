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
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Client
{
    internal class DefaultJobCreationProcess : IJobCreationProcess
    {
        public static DefaultJobCreationProcess Instance { get; private set; }

        static DefaultJobCreationProcess()
        {
            Instance = new DefaultJobCreationProcess();
        }

        private readonly Func<Job, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public DefaultJobCreationProcess()
        {
        }

        internal DefaultJobCreationProcess(IEnumerable<object> filters)
            : this()
        {
            _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
        }

        public string Run(CreateContext context, IStateMachine stateMachine)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (stateMachine == null) throw new ArgumentNullException("stateMachine");

            var filterInfo = GetFilters(context.Job);

            try
            {
                var createdContext = CreateWithFilters(context, stateMachine, filterInfo.ClientFilters);
                return createdContext.JobId;
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
            return new JobFilterInfo(_getFiltersThunk(job));
        }

        private static CreatedContext CreateWithFilters(
            CreateContext context,
            IStateMachine stateMachine,
            IEnumerable<IClientFilter> filters)
        {
            var preContext = new CreatingContext(context);
            Func<CreatedContext> continuation = () =>
            {
                var jobId = stateMachine.CreateInState(context.Job, context.Parameters, context.InitialState);
                return new CreatedContext(context, jobId, false, null);
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