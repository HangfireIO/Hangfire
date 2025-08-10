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
            : this(context.Storage, context.Connection, transaction, context.BackgroundJob, context.CandidateState, context.CurrentState, context.Logger, context.Profiler, context.StateMachine, context.CustomData != null ? new Dictionary<string, object?>(context.CustomData) : null)
        {
            // TODO: Add explicit JobExpirationTimeout parameter in 2.0, because it's unclear it isn't preserved
        }

        [Obsolete("Please use an overload with the `ILog` parameter instead. Will be removed in 2.0.0.")]
        public ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string? oldStateName)
            : this(storage, connection, transaction, backgroundJob, newState, oldStateName, LogProvider.NoOpLogger.Instance)
        {
        }

        public ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState,
            [CanBeNull] string? oldStateName,
            [NotNull] ILog logger)
            : this(storage, connection, transaction, backgroundJob, newState, oldStateName, logger, EmptyProfiler.Instance, null)
        {
        }

        internal ApplyStateContext(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection,
            [NotNull] IWriteOnlyTransaction transaction,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IState newState, 
            [CanBeNull] string? oldStateName,
            [NotNull] ILog logger,
            [NotNull] IProfiler profiler,
            [CanBeNull] IStateMachine? stateMachine,
            [CanBeNull] IReadOnlyDictionary<string, object?>? customData = null)
        {
            BackgroundJob = backgroundJob ?? throw new ArgumentNullException(nameof(backgroundJob));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            NewState = newState ?? throw new ArgumentNullException(nameof(newState));
            OldStateName = oldStateName;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            StateMachine = stateMachine;
            CustomData = customData;
            JobExpirationTimeout = storage.JobExpirationTimeout;
        }

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
        public ILog Logger { get; }

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