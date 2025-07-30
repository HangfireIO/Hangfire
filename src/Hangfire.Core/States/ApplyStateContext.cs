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

namespace Hangfire.States
{
#pragma warning disable 618
    public class ApplyStateContext : StateContext
#pragma warning restore 618
    {
        public ApplyStateContext(
            [NotNull] IWriteOnlyTransaction transaction, 
            [NotNull] ElectStateContext context)
            : this(context.Configuration, context.Connection, transaction, context.BackgroundJob, context.CandidateState, context.CurrentState, context.CustomData != null ? new Dictionary<string, object?>(context.CustomData) : null)
        {
            // TODO: Add explicit JobExpirationTimeout parameter in 2.0, because it's unclear it isn't preserved
        }

        [Obsolete("Please use the overload that takes an IBackgroundConfiguration instead of JobStorage.")]
        public ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string? oldStateName)
            : this(BackgroundConfiguration.Instance.WithJobStorage(_ => storage), connection, transaction, backgroundJob, newState, oldStateName, null)
        {
        }

        public ApplyStateContext(
            [NotNull] IBackgroundConfiguration configuration,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string? oldStateName)
            : this(configuration, connection, transaction, backgroundJob, newState, oldStateName, null)
        {
        }

        internal ApplyStateContext(
            [NotNull] IBackgroundConfiguration configuration,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState, 
            [CanBeNull] string? oldStateName,
            [CanBeNull] IReadOnlyDictionary<string, object?>? customData = null)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Storage = configuration.Resolve<JobStorage>(); 
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            BackgroundJob = backgroundJob ?? throw new ArgumentNullException(nameof(backgroundJob));
            NewState = newState ?? throw new ArgumentNullException(nameof(newState));
            OldStateName = oldStateName;
            Profiler = configuration.Resolve<IProfiler>();
            StateMachine = configuration.Resolve<IStateMachine>();
            CustomData = customData;
            JobExpirationTimeout = Storage.JobExpirationTimeout;
        }

        [NotNull]
        public IBackgroundConfiguration Configuration { get; }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        [NotNull]
        public IWriteOnlyTransaction Transaction { get; }
        
        public override BackgroundJob BackgroundJob { get; }

        [CanBeNull]
        public string? OldStateName { get; }

        [NotNull]
        public IState NewState { get; }
        
        public TimeSpan JobExpirationTimeout { get; set; }

        [NotNull]
        internal IProfiler Profiler { get; }

        [CanBeNull]
        public IReadOnlyDictionary<string, object?>? CustomData { get; }

        [CanBeNull]
        public IStateMachine? StateMachine { get; }

        [CanBeNull]
        public T? GetJobParameter<T>([NotNull] string name) => GetJobParameter<T>(name, allowStale: false);

        [CanBeNull]
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
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not get a value of the job parameter `{name}`. See inner exception for details.", ex);
            }
        }
    }
}