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
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.States
{
    public class DefaultStateChangeProcess : IStateChangeProcess
    {
        private readonly StateHandlerCollection _handlers;
        private readonly IJobFilterProvider _filterProvider;

        public DefaultStateChangeProcess([NotNull] StateHandlerCollection handlers)
            : this(handlers, JobFilterProviders.Providers)
        {
        }

        public DefaultStateChangeProcess(
            [NotNull] StateHandlerCollection handlers,
            [NotNull] IJobFilterProvider filterProvider)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");

            _handlers = handlers;
            _filterProvider = filterProvider;
        }

        public void ElectState(ElectStateContext context)
        {
            var filterInfo = GetFilters(context.BackgroundJob.Job);
            foreach (var filter in filterInfo.ElectStateFilters)
            {
                filter.OnStateElection(context);
            }
        }

        public void ApplyState(ApplyStateContext context)
        {
            var filterInfo = GetFilters(context.BackgroundJob.Job);
            var filters = filterInfo.ApplyStateFilters;

            foreach (var state in context.TraversedStates)
            {
                context.Transaction.AddJobState(context.BackgroundJob.Id, state);
            }

            foreach (var handler in _handlers.GetHandlers(context.OldStateName))
            {
                handler.Unapply(context, context.Transaction);
            }

            foreach (var filter in filters)
            {
                filter.OnStateUnapplied(context, context.Transaction);
            }

            context.Transaction.SetJobState(context.BackgroundJob.Id, context.NewState);

            foreach (var handler in _handlers.GetHandlers(context.NewState.Name))
            {
                handler.Apply(context, context.Transaction);
            }

            foreach (var filter in filters)
            {
                filter.OnStateApplied(context, context.Transaction);
            }

            if (context.NewState.IsFinal)
            {
                context.Transaction.ExpireJob(context.BackgroundJob.Id, context.JobExpirationTimeout);
            }
            else
            {
                context.Transaction.PersistJob(context.BackgroundJob.Id);
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }
    }
}