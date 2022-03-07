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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Client
{
    /// <summary>
    /// Provides information about the context in which the job is created.
    /// </summary>
    public class CreateContext
    {
        public CreateContext([NotNull] CreateContext context)
            : this(context.Storage, context.Connection, context.Job, context.InitialState, context.Profiler)
        {
            Items = context.Items;
            Parameters = context.Parameters;
        }

        public CreateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] Job job,
            [CanBeNull] IState initialState)
            : this(storage, connection, job, initialState, EmptyProfiler.Instance)
        {
        }

        internal CreateContext(
            [NotNull] JobStorage storage, 
            [NotNull] IStorageConnection connection, 
            [NotNull] Job job, 
            [CanBeNull] IState initialState,
            [NotNull] IProfiler profiler)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (job == null) throw new ArgumentNullException(nameof(job));

            Storage = storage;
            Connection = connection;
            Job = job;
            InitialState = initialState;
            Profiler = profiler;

            Items = new Dictionary<string, object>();
            Parameters = new Dictionary<string, object>();
        }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        /// <summary>
        /// Gets an instance of the key-value storage. You can use it
        /// to pass additional information between different client filters
        /// or just between different methods.
        /// </summary>
        [NotNull]
        public IDictionary<string, object> Items { get; }

        [NotNull]
        public virtual IDictionary<string, object> Parameters { get; }
            
        [NotNull]
        public Job Job { get; }

        /// <summary>
        /// Gets the initial state of the creating job. Note, that
        /// the final state of the created job could be changed after 
        /// the registered instances of the <see cref="IElectStateFilter"/>
        /// class are doing their job.
        /// </summary>
        [CanBeNull]
        public IState InitialState { get; }

        [NotNull]
        internal IProfiler Profiler { get; }
    }
}