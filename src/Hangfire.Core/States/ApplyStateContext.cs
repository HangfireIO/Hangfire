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
            : this(context.Storage, context.Connection, transaction, context.BackgroundJob, context.CandidateState, context.CurrentState, context.Profiler)
        {
        }

        public ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string oldStateName)
            : this(storage, connection, transaction, backgroundJob, newState, oldStateName, EmptyProfiler.Instance)
        {
        }

        internal ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState, 
            [CanBeNull] string oldStateName,
            [NotNull] IProfiler profiler)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (backgroundJob == null) throw new ArgumentNullException(nameof(backgroundJob));
            if (newState == null) throw new ArgumentNullException(nameof(newState));
            
            BackgroundJob = backgroundJob;

            Storage = storage;
            Connection = connection;
            Transaction = transaction;
            OldStateName = oldStateName;
            NewState = newState;
            JobExpirationTimeout = storage.JobExpirationTimeout;
            Profiler = profiler;
        }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        [NotNull]
        public IWriteOnlyTransaction Transaction { get; }
        
        public override BackgroundJob BackgroundJob { get; }

        [CanBeNull]
        public string OldStateName { get; }

        [NotNull]
        public IState NewState { get; }
        
        public TimeSpan JobExpirationTimeout { get; set; }

        [NotNull]
        internal IProfiler Profiler { get; }
    }
}