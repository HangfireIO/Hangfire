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
using HangFire.Storage;

namespace HangFire.Common.States
{
    public class StateApplyingContext : StateContext
    {
        private static readonly TimeSpan JobExpirationTimeout = TimeSpan.FromDays(1);
        private readonly IStorageConnection _connection;

        internal StateApplyingContext(
            StateContext context,
            IStorageConnection connection,
            State newState,
            string oldStateName)
            : base(context)
        {
            _connection = connection;
            OldStateName = oldStateName;
            NewState = newState;
        }

        public string OldStateName { get; private set; }
        public State NewState { get; private set; }

        internal virtual bool ApplyState(
            StateHandlerCollection handlers,
            IEnumerable<IStateChangedFilter> filters)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            if (filters == null) throw new ArgumentNullException("filters");

            using (var transaction = _connection.CreateWriteTransaction())
            {
                foreach (var handler in handlers.GetHandlers(OldStateName))
                {
                    handler.Unapply(this, transaction);
                }

                foreach (var filter in filters)
                {
                    filter.OnStateUnapplied(this, transaction);
                }

                transaction.SetJobState(JobId, NewState);

                foreach (var handler in handlers.GetHandlers(NewState.Name))
                {
                    handler.Apply(this, transaction);
                }

                foreach (var filter in filters)
                {
                    filter.OnStateApplied(this, transaction);
                }

                if (NewState.ExpireJobOnApply)
                {
                    transaction.ExpireJob(JobId, JobExpirationTimeout);
                }
                else
                {
                    transaction.PersistJob(JobId);
                }

                return transaction.Commit();
            }
        }
    }
}