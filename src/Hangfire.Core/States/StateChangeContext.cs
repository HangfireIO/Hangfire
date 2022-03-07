// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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