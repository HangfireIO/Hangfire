// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
        : this(storage, connection, backgroundJobId, newState, expectedStates, false, cancellationToken, EmptyProfiler.Instance)
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
            [NotNull] IProfiler profiler)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (backgroundJobId == null) throw new ArgumentNullException(nameof(backgroundJobId));
            if (newState == null) throw new ArgumentNullException(nameof(newState));
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));

            Storage = storage;
            Connection = connection;
            BackgroundJobId = backgroundJobId;
            NewState = newState;
            ExpectedStates = expectedStates;
            DisableFilters = disableFilters;
            CancellationToken = cancellationToken;
            Profiler = profiler;
        }

        public JobStorage Storage { get; }
        public IStorageConnection Connection { get; }
        public string BackgroundJobId { get; }
        public IState NewState { get; }
        public IEnumerable<string> ExpectedStates { get; }
        public bool DisableFilters { get; }
        public CancellationToken CancellationToken { get; }
        internal IProfiler Profiler { get; }
    }
}