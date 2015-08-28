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
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class ApplyStateContext : StateContext
    {
        private readonly BackgroundJob _backgroundJob;

        public ApplyStateContext(
            [NotNull] IWriteOnlyTransaction transaction, 
            [NotNull] ElectStateContext context)
            : this(context.Storage, context.Connection, transaction, context.BackgroundJob, context.CandidateState, context.CurrentState, context.TraversedStates)
        {
        }

        public ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState, 
            [CanBeNull] string oldStateName, 
            [NotNull] IEnumerable<IState> traversedStates)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (connection == null) throw new ArgumentNullException("connection");
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (backgroundJob == null) throw new ArgumentNullException("backgroundJob");
            if (newState == null) throw new ArgumentNullException("newState");
            if (traversedStates == null) throw new ArgumentNullException("traversedStates");
            
            _backgroundJob = backgroundJob;

            Storage = storage;
            Connection = connection;
            Transaction = transaction;
            OldStateName = oldStateName;
            NewState = newState;
            TraversedStates = traversedStates;
            JobExpirationTimeout = TimeSpan.FromDays(1);
        }

        [NotNull]
        public JobStorage Storage { get; private set; }

        [NotNull]
        public IStorageConnection Connection { get; private set; }

        [NotNull]
        public IWriteOnlyTransaction Transaction { get; private set; }

        [NotNull]
        public override BackgroundJob BackgroundJob { get { return _backgroundJob; } }

        [CanBeNull]
        public string OldStateName { get; private set; }

        [NotNull]
        public IState NewState { get; private set; }
        
        [NotNull]
        public IEnumerable<IState> TraversedStates { get; private set; } 
        
        public TimeSpan JobExpirationTimeout { get; set; }
    }
}