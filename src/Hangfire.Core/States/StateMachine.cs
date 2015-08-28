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
        private readonly IStateMachine _innerStateMachine;

        public StateMachine()
            : this(JobFilterProviders.Providers)
        {
        }

        public StateMachine([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, new CoreStateMachine())
        {
        }

        internal StateMachine(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IStateMachine innerStateMachine)
        {
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");
            if (innerStateMachine == null) throw new ArgumentNullException("innerStateMachine");
            
            _filterProvider = filterProvider;
            _innerStateMachine = innerStateMachine;
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

            foreach (var filter in filters)
            {
                filter.OnStateUnapplied(context, context.Transaction);
            }

            _innerStateMachine.ApplyState(context);

            foreach (var filter in filters)
            {
                filter.OnStateApplied(context, context.Transaction);
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }
    }
}