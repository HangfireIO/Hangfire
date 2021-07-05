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
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Profiling;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class StateChangeContext
    {
        public StateChangeContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] string backgroundJobId, 
            [NotNull] IState newState)
            : this(storage, connection, backgroundJobId, newState, null)
        {
        }

        public StateChangeContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] string backgroundJobId, 
            [NotNull] IState newState, 
            [CanBeNull] params string[] expectedStates)
            : this(storage, connection, backgroundJobId, newState, expectedStates, CancellationToken.None)
        {
        }

        public StateChangeContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] string backgroundJobId,
            [NotNull] IState newState,
            [CanBeNull] IEnumerable<string> expectedStates,
            CancellationToken cancellationToken)
        : this(storage, connection, null, backgroundJobId, newState, expectedStates, false, null, cancellationToken, EmptyProfiler.Instance)
        {
        }

        public StateChangeContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [CanBeNull] JobStorageTransaction transaction,
            [NotNull] string backgroundJobId,
            [NotNull] IState newState,
            [CanBeNull] IEnumerable<string> expectedStates,
            CancellationToken cancellationToken)
            : this(storage, connection, transaction, backgroundJobId, newState, expectedStates, false, null, cancellationToken, EmptyProfiler.Instance)
        {
        }

        internal StateChangeContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] string backgroundJobId,
            [NotNull] IState newState,
            [CanBeNull] IEnumerable<string> expectedStates,
            bool disableFilters,
            CancellationToken cancellationToken,
            [NotNull] IProfiler profiler,
            [CanBeNull] IReadOnlyDictionary<string, object> customData = null) 
            : this(storage, connection, null, backgroundJobId, newState, expectedStates, disableFilters, null, cancellationToken, profiler, customData)
        {
        }

        internal StateChangeContext(
            [NotNull] JobStorage storage, 
            [NotNull] IStorageConnection connection,
            [CanBeNull] JobStorageTransaction transaction,
            [NotNull] string backgroundJobId, 
            [NotNull] IState newState, 
            [CanBeNull] IEnumerable<string> expectedStates,
            bool disableFilters,
            [CanBeNull] IFetchedJob completeJob,
            CancellationToken cancellationToken,
            [NotNull] IProfiler profiler,
            [CanBeNull] IReadOnlyDictionary<string, object> customData = null)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction;
            BackgroundJobId = backgroundJobId ?? throw new ArgumentNullException(nameof(backgroundJobId));
            NewState = newState ?? throw new ArgumentNullException(nameof(newState));
            ExpectedStates = expectedStates;
            DisableFilters = disableFilters;
            CancellationToken = cancellationToken;
            Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            CustomData = customData;
            CompleteJob = completeJob;
        }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        [CanBeNull]
        public JobStorageTransaction Transaction { get; }

        [NotNull]
        public string BackgroundJobId { get; }

        [NotNull]
        public IState NewState { get; }

        [CanBeNull]
        public IEnumerable<string> ExpectedStates { get; }
        
        public bool DisableFilters { get; }
        public CancellationToken CancellationToken { get; }

        [NotNull]
        internal IProfiler Profiler { get; }

        [CanBeNull]
        public IReadOnlyDictionary<string, object> CustomData { get; }

        [CanBeNull]
        public IFetchedJob CompleteJob { get; }
        
        [CanBeNull]
        public BackgroundJob ProcessedJob { get; set; }
    }
}