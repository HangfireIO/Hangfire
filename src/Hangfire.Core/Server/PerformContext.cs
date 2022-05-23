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
            : this(context.Storage, context.Connection, context.BackgroundJob, context.CancellationToken, context.Profiler)
        {
            Items = context.Items;
        }

        [Obsolete("Please use PerformContext(JobStorage, IStorageConnection, BackgroundJob, IJobCancellationToken) overload instead. Will be removed in 2.0.0.")]
        public PerformContext(
            [NotNull] IStorageConnection connection,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken)
            : this(null, connection, backgroundJob, cancellationToken)
        {
        }

        public PerformContext(
            [CanBeNull] JobStorage storage,
            [NotNull] IStorageConnection connection, 
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken)
            : this(storage, connection, backgroundJob, cancellationToken, EmptyProfiler.Instance)
        {
        }

        internal PerformContext(
            [CanBeNull] JobStorage storage,
            [NotNull] IStorageConnection connection, 
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken,
            [NotNull] IProfiler profiler)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (backgroundJob == null) throw new ArgumentNullException(nameof(backgroundJob));
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));

            Storage = storage;
            Connection = connection;
            BackgroundJob = backgroundJob;
            CancellationToken = cancellationToken;
            Profiler = profiler;

            Items = new Dictionary<string, object>();
        }

        [CanBeNull]
        public JobStorage Storage { get; }

        /// <summary>
        /// Gets an instance of the key-value storage. You can use it
        /// to pass additional information between different client filters
        /// or just between different methods.
        /// </summary>
        [NotNull]
        public IDictionary<string, object> Items { get; }

        [NotNull]
        public BackgroundJob BackgroundJob { get; }

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public string JobId => BackgroundJob.Id;

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public Job Job => BackgroundJob.Job;

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public DateTime CreatedAt => BackgroundJob.CreatedAt;

        [NotNull]
        public IJobCancellationToken CancellationToken { get; }

        [NotNull]
        public IStorageConnection Connection { get; }
        
        [NotNull]
        internal IProfiler Profiler { get; }

        public void SetJobParameter(string name, object value)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Connection.SetJobParameter(BackgroundJob.Id, name, SerializationHelper.Serialize(value, SerializationOption.User));
        }

        public T GetJobParameter<T>(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            try
            {
                return SerializationHelper.Deserialize<T>(Connection.GetJobParameter(BackgroundJob.Id, name), SerializationOption.User);
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                throw new InvalidOperationException(
                    $"Could not get a value of the job parameter `{name}`. See inner exception for details.", ex);
            }
        }
    }
}
