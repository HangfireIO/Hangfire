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

namespace Hangfire.States
{
    internal class StateChangeProcess : IStateChangeProcess
    {
        private readonly StateHandlerCollection _handlers;

        private readonly Func<Job, IEnumerable<JobFilter>> _getFiltersThunk
            = JobFilterProviders.Providers.GetFilters;

        public StateChangeProcess(StateHandlerCollection handlers)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");

            _handlers = handlers;
        }

        internal StateChangeProcess(StateHandlerCollection handlers, IEnumerable<object> filters)
            : this(handlers)
        {
            if (filters == null) throw new ArgumentNullException("filters");

            _getFiltersThunk = md => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
        }

        public bool ChangeState(StateContext context, IState toState, string oldStateName)
        {
            try
            {
                var filterInfo = GetFilters(context.Job);

                var electStateContext = new ElectStateContext(context, toState, oldStateName);
                var electedState = ElectState(electStateContext, filterInfo.ElectStateFilters);

                var applyStateContext = new ApplyStateContext(context, electedState, oldStateName);
                ApplyState(applyStateContext, filterInfo.ApplyStateFilters);

                // State transition was succeeded.
                return true;
            }
            catch (Exception ex)
            {
                var failedState = new FailedState(ex)
                {
                    Reason = "An exception occurred during the transition of job's state"
                };

                var applyStateContext = new ApplyStateContext(context, failedState, oldStateName);

                // We should not use any state changed filters, because
                // some of the could cause an exception.
                ApplyState(applyStateContext, Enumerable.Empty<IApplyStateFilter>());

                // State transition was failed due to exception.
                return false;
            }
        }

        private static IState ElectState(
            ElectStateContext context, 
            IEnumerable<IElectStateFilter> filters)
        {
            var statesToAppend = new List<IState>();

            foreach (var filter in filters)
            {
                var oldState = context.CandidateState;
                filter.OnStateElection(context);

                if (oldState != context.CandidateState)
                {
                    statesToAppend.Add(oldState);
                }
            }

            if (statesToAppend.Count > 0)
            {
                using (var transaction = context.Connection.CreateWriteTransaction())
                {
                    foreach (var state in statesToAppend)
                    {
                        transaction.AddJobState(context.JobId, state);
                    }

                    transaction.Commit();
                }
            }

            return context.CandidateState;
        }

        private void ApplyState(ApplyStateContext context, IEnumerable<IApplyStateFilter> filters)
        {
            using (var transaction = context.Connection.CreateWriteTransaction())
            {
                foreach (var handler in _handlers.GetHandlers(context.OldStateName))
                {
                    handler.Unapply(context, transaction);
                }

                foreach (var filter in filters)
                {
                    filter.OnStateUnapplied(context, transaction);
                }

                transaction.SetJobState(context.JobId, context.NewState);

                foreach (var handler in _handlers.GetHandlers(context.NewState.Name))
                {
                    handler.Apply(context, transaction);
                }

                foreach (var filter in filters)
                {
                    filter.OnStateApplied(context, transaction);
                }

                if (context.NewState.IsFinal)
                {
                    transaction.ExpireJob(context.JobId, context.JobExpirationTimeout);
                }
                else
                {
                    transaction.PersistJob(context.JobId);
                }

                transaction.Commit();
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_getFiltersThunk(job));
        }
    }
}