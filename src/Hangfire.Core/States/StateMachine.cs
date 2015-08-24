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
    public class StateMachine : IStateMachine
    {
        private readonly IJobFilterProvider _filterProvider;
        private readonly Func<JobStorage, StateHandlerCollection> _stateHandlersThunk;

        public StateMachine()
            : this(JobFilterProviders.Providers)
        {
        }

        public StateMachine([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, GetStateHandlers)
        {
        }

        internal StateMachine(
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] Func<JobStorage, StateHandlerCollection> stateHandlersThunk)
        {
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");
            if (stateHandlersThunk == null) throw new ArgumentNullException("stateHandlersThunk");
            
            _filterProvider = filterProvider;
            _stateHandlersThunk = stateHandlersThunk;
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
            var handlers = _stateHandlersThunk(context.Storage);

            foreach (var state in context.TraversedStates)
            {
                context.Transaction.AddJobState(context.BackgroundJob.Id, state);
            }

            foreach (var handler in handlers.GetHandlers(context.OldStateName))
            {
                handler.Unapply(context, context.Transaction);
            }

            foreach (var filter in filters)
            {
                filter.OnStateUnapplied(context, context.Transaction);
            }

            context.Transaction.SetJobState(context.BackgroundJob.Id, context.NewState);

            foreach (var handler in handlers.GetHandlers(context.NewState.Name))
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

        private static StateHandlerCollection GetStateHandlers(JobStorage storage)
        {
            var stateHandlers = new StateHandlerCollection();
            stateHandlers.AddRange(GlobalStateHandlers.Handlers);
            stateHandlers.AddRange(storage.GetStateHandlers());

            return stateHandlers;
        }
    }
}