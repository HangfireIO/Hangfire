// This file is part of Hangfire.
// Copyright � 2013-2014 Sergey Odinokov.
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
    // TODO: Merge this class with BackgroundJobStateChanger in 2.0.0
    public class StateMachine : IStateMachine
    {
        private readonly IJobFilterProvider _filterProvider;
        private readonly IStateMachine _innerStateMachine;

        public StateMachine([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, new CoreStateMachine())
        {
        }

        internal StateMachine(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IStateMachine innerStateMachine)
        {
            if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
            if (innerStateMachine == null) throw new ArgumentNullException(nameof(innerStateMachine));
            
            _filterProvider = filterProvider;
            _innerStateMachine = innerStateMachine;
        }

        public IState ApplyState(ApplyStateContext initialContext)
        {
            var filterInfo = GetFilters(initialContext.BackgroundJob.Job);
            var electFilters = filterInfo.ElectStateFilters;
            var applyFilters = filterInfo.ApplyStateFilters;

            // Electing a a state
            var electContext = new ElectStateContext(initialContext);

            foreach (var filter in electFilters)
            {
                filter.OnStateElection(electContext);
            }

            foreach (var state in electContext.TraversedStates)
            {
                initialContext.Transaction.AddJobState(electContext.BackgroundJob.Id, state);
            }

            // Applying the elected state
            var context = new ApplyStateContext(initialContext.Transaction, electContext)
            {
                JobExpirationTimeout = initialContext.JobExpirationTimeout
            };

            foreach (var filter in applyFilters)
            {
                filter.OnStateUnapplied(context, context.Transaction);
            }

            foreach (var filter in applyFilters)
            {
                filter.OnStateApplied(context, context.Transaction);
            }

            return _innerStateMachine.ApplyState(context);
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }
    }
}