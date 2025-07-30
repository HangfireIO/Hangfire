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
using Hangfire.Profiling;
using Hangfire.Storage;

namespace Hangfire.Server
{
    /// <summary>
    /// Provides information about the context in which the job
    /// is performed.
    /// </summary>
    public class PerformContext
    {
        public PerformContext([NotNull] PerformContext context)
            : this(context.Configuration, context.Connection, context.BackgroundJob, context.CancellationToken, context.Profiler, context.ServerId, context.Items)
        {
            Performer = context.Performer;
        }

        [Obsolete("Please use PerformContext(JobStorage, IStorageConnection, BackgroundJob, IJobCancellationToken) overload instead. Will be removed in 2.0.0.")]
        public PerformContext(
            [NotNull] IStorageConnection connection,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken)
            : this(BackgroundConfiguration.Instance, connection, backgroundJob, cancellationToken)
        {
        }

        [Obsolete("Please use the overload that takes an IBackgroundConfiguration instead of JobStorage.")]
        public PerformContext(
            [CanBeNull] JobStorage? storage,
            [NotNull] IStorageConnection connection, 
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken)
            : this(BackgroundConfiguration.Instance.WithJobStorage(_ => storage ?? JobStorage.Current), connection, backgroundJob, cancellationToken, EmptyProfiler.Instance, null, null)
        {
        }

        public PerformContext(
            [NotNull] IBackgroundConfiguration configuration,
            [NotNull] IStorageConnection connection, 
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken)
            : this(configuration, connection, backgroundJob, cancellationToken, EmptyProfiler.Instance, null, null)
        {
        }

        internal PerformContext(
            [NotNull] IBackgroundConfiguration configuration,
            [NotNull] IStorageConnection connection, 
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken,
            [NotNull] IProfiler profiler, // TODO: Resolve
            [CanBeNull] string? serverId, // TODO: Resolve
            [CanBeNull] IDictionary<string, object?>? items)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Storage = configuration.Resolve<JobStorage>();
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            BackgroundJob = backgroundJob ?? throw new ArgumentNullException(nameof(backgroundJob));
            CancellationToken = cancellationToken ?? throw new ArgumentNullException(nameof(cancellationToken));
            Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            ServerId = serverId;

            Items = items ?? new Dictionary<string, object?>();
        }
        
        [NotNull]
        public IBackgroundConfiguration Configuration { get; }

        [NotNull]
        public JobStorage Storage { get; }

        /// <summary>
        /// Gets an instance of the key-value storage. You can use it
        /// to pass additional information between different client filters
        /// or just between different methods.
        /// </summary>
        [NotNull]
        public IDictionary<string, object?> Items { get; }

        [NotNull]
        public BackgroundJob BackgroundJob { get; }

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public string JobId => BackgroundJob.Id;

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        [CanBeNull]
        public Job? Job => BackgroundJob.Job;

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public DateTime CreatedAt => BackgroundJob.CreatedAt;

        [NotNull]
        public IJobCancellationToken CancellationToken { get; }

        [NotNull]
        public IStorageConnection Connection { get; }
        
        [NotNull]
        internal IProfiler Profiler { get; }

        [CanBeNull]
        public IBackgroundJobPerformer? Performer { get; internal set; }

        [CanBeNull]
        public string? ServerId { get; }

        public void SetJobParameter([NotNull] string name, object? value)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Connection.SetJobParameter(BackgroundJob.Id, name, SerializationHelper.Serialize(value, SerializationOption.User));
        }

        public T? GetJobParameter<T>([NotNull] string name) => GetJobParameter<T>(name, allowStale: false);

        public T? GetJobParameter<T>([NotNull] string name, bool allowStale)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            try
            {
                string? value;

                if (allowStale && BackgroundJob.ParametersSnapshot != null)
                {
                    BackgroundJob.ParametersSnapshot.TryGetValue(name, out value);
                }
                else
                {
                    value = Connection.GetJobParameter(BackgroundJob.Id, name);                
                }

                return SerializationHelper.Deserialize<T>(value, SerializationOption.User);
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                throw new InvalidOperationException(
                    $"Could not get a value of the job parameter `{name}`. See inner exception for details.", ex);
            }
        }
    }
}
