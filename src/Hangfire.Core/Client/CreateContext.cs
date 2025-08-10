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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Logging;
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
            : this(context.Storage, context.Connection, context.Job, context.InitialState, context.Parameters, context.Logger, context.Profiler, context.Items)
        {
            Factory = context.Factory;
        }

        [Obsolete("Please use a non-obsolete overload instead. Will be removed in 2.0.0.")]
        public CreateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] Job job,
            [CanBeNull] IState? initialState)
            : this(storage, connection, job, initialState, null)
        {
        }

        [Obsolete("Please use an overload with the `ILog` parameter added instead. Will be removed in 2.0.0.")]
        public CreateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] Job job,
            [CanBeNull] IState? initialState,
            [CanBeNull] IDictionary<string, object?>? parameters)
            : this(storage, connection, job, initialState, parameters, LogProvider.NoOpLogger.Instance)
        {
        }

        public CreateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] Job job,
            [CanBeNull] IState? initialState,
            [CanBeNull] IDictionary<string, object?>? parameters,
            [NotNull] ILog logger)
            : this(storage, connection, job, initialState, parameters, logger, EmptyProfiler.Instance, null)
        {
        }

        internal CreateContext(
            [NotNull] JobStorage storage, 
            [NotNull] IStorageConnection connection, 
            [NotNull] Job job, 
            [CanBeNull] IState? initialState,
            [CanBeNull] IDictionary<string, object?>? parameters,
            [NotNull] ILog logger,
            [NotNull] IProfiler profiler,
            [CanBeNull] IDictionary<string, object?>? items)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Job = job ?? throw new ArgumentNullException(nameof(job));
            InitialState = initialState;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            Items = items ?? new Dictionary<string, object?>();
            Parameters = parameters ?? new Dictionary<string, object?>();
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
        public IDictionary<string, object?> Items { get; }

        [NotNull]
        public virtual IDictionary<string, object?> Parameters { get; }
            
        [NotNull]
        public Job Job { get; }

        /// <summary>
        /// Gets the initial state of the creating job. Note, that
        /// the final state of the created job could be changed after 
        /// the registered instances of the <see cref="IElectStateFilter"/>
        /// class are doing their job.
        /// </summary>
        [CanBeNull]
        public IState? InitialState { get; }

        [NotNull]
        public ILog Logger { get; }

        [NotNull]
        internal IProfiler Profiler { get; }

        [CanBeNull]
        public IBackgroundJobFactory? Factory { get; internal set; }
    }
}