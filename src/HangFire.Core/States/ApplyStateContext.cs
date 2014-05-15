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
using HangFire.Storage;

namespace HangFire.States
{
    public class ApplyStateContext : StateContext
    {
        public ApplyStateContext(
            StateContext context,
            IState newState,
            string oldStateName)
            : base(context)
        {
            if (newState == null) throw new ArgumentNullException("newState");

            OldStateName = oldStateName;
            NewState = newState;
            JobExpirationTimeout = TimeSpan.FromDays(1);
        }
        
        // Hiding the connection from filters, because their methods are being 
        // executed inside a transaction. This property can break them.
        private new IStorageConnection Connection { get { return base.Connection; } }

        public string OldStateName { get; private set; }
        public IState NewState { get; private set; }
        public TimeSpan JobExpirationTimeout { get; set; }

        internal void ApplyState(
            StateHandlerCollection handlers, IEnumerable<IApplyStateFilter> filters)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            if (filters == null) throw new ArgumentNullException("filters");

            using (var transaction = Connection.CreateWriteTransaction())
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

                if (NewState.IsFinal)
                {
                    transaction.ExpireJob(JobId, JobExpirationTimeout);
                }
                else
                {
                    transaction.PersistJob(JobId);
                }

                transaction.Commit();
            }
        }
    }
}