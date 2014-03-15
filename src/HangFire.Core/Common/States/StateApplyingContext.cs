// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using HangFire.Storage;

namespace HangFire.Common.States
{
    public class StateApplyingContext : StateContext
    {
        internal StateApplyingContext(
            StateContext context, 
            IWriteOnlyTransaction transaction,
            string oldStateName,
            JobState newState)
            : base(context)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (oldStateName == null) throw new ArgumentNullException("oldStateName");
            if (newState == null) throw new ArgumentNullException("newState");

            Transaction = transaction;
            OldStateName = oldStateName;
            NewState = newState;
        }

        public IWriteOnlyTransaction Transaction { get; private set; }

        public string OldStateName { get; private set; }
        public JobState NewState { get; private set; }

        public void ApplyState(
            IDictionary<string, List<JobStateHandler>> handlers,
            IEnumerable<IStateChangedFilter> filters)
        {
            foreach (var handler in GetHandlers(OldStateName, handlers))
            {
                handler.Unapply(this);
            }

            foreach (var filter in filters)
            {
                filter.OnStateUnapplied(this);
            }

            Transaction.SetJobState(JobId, NewState, JobMethod);

            foreach (var handler in GetHandlers(NewState.StateName, handlers))
            {
                handler.Apply(this);
            }

            foreach (var filter in filters)
            {
                filter.OnStateApplied(this);
            }
        }

        private IEnumerable<JobStateHandler> GetHandlers(
            string stateName, IDictionary<string, List<JobStateHandler>> handlers)
        {
            if (handlers.ContainsKey(stateName))
            {
                return handlers[stateName];
            }

            return Enumerable.Empty<JobStateHandler>();
        }
    }
}