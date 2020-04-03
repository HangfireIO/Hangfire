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
using Hangfire.Profiling;
using Hangfire.Storage;

namespace Hangfire.States
{
#pragma warning disable 618
    public class ApplyStateContext : StateContext
#pragma warning restore 618
    {
        public ApplyStateContext(
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] ElectStateContext context)
            : this(context.Storage, context.Clock, context.Connection, transaction, context.BackgroundJob, context.CandidateState, context.CurrentState, context.Profiler)
        {
        }

        public ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IClock clock,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string oldStateName)
            : this(storage, clock, connection, transaction, backgroundJob, newState, oldStateName, EmptyProfiler.Instance)
        {
        }

        internal ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IClock clock,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string oldStateName,
            [NotNull] IProfiler profiler)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (backgroundJob == null) throw new ArgumentNullException(nameof(backgroundJob));
            if (newState == null) throw new ArgumentNullException(nameof(newState));

            BackgroundJob = backgroundJob;

            Storage = storage;
            Clock = clock;
            Connection = connection;
            Transaction = transaction;
            OldStateName = oldStateName;
            NewState = newState;
            JobExpirationTimeout = storage.JobExpirationTimeout;
            Profiler = profiler;
        }

        [NotNull] public JobStorage Storage { get; }

        [NotNull] public IClock Clock { get; }

        [NotNull] public IStorageConnection Connection { get; }

        [NotNull] public IWriteOnlyTransaction Transaction { get; }

        public override BackgroundJob BackgroundJob { get; }

        [CanBeNull] public string OldStateName { get; }

        [NotNull] public IState NewState { get; }

        public TimeSpan JobExpirationTimeout { get; set; }

        [NotNull] internal IProfiler Profiler { get; }
    }
}
