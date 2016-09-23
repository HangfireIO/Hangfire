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
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;

namespace Hangfire
{
    public class ContinuationsSupportAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
    {
        private static readonly TimeSpan AddJobLockTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan ContinuationStateFetchTimeout = TimeSpan.FromSeconds(30);

        private static readonly ILog Logger = LogProvider.For<ContinuationsSupportAttribute>();

        private readonly HashSet<string> _knownFinalStates;
        private readonly IBackgroundJobStateChanger _stateChanger;

        public ContinuationsSupportAttribute()
            : this(new HashSet<string> { DeletedState.StateName, SucceededState.StateName })
        {
        }

        public ContinuationsSupportAttribute(HashSet<string> knownFinalStates)
            : this(knownFinalStates, new BackgroundJobStateChanger())
        {
        }

        public ContinuationsSupportAttribute(
            [NotNull] HashSet<string> knownFinalStates, 
            [NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (knownFinalStates == null) throw new ArgumentNullException(nameof(knownFinalStates));
            if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));

            _knownFinalStates = knownFinalStates;
            _stateChanger = stateChanger;

            // Ensure this filter is the last filter in the chain to start
            // continuations on the last candidate state only.
            Order = 1000;
        }

        public void OnStateElection(ElectStateContext context)
        {
            var awaitingState = context.CandidateState as AwaitingState;
            if (awaitingState != null)
            {
                // Branch for a child background job.
                AddContinuation(context, awaitingState);
            }
            else if (_knownFinalStates.Contains(context.CandidateState.Name))
            {
                // Branch for a parent background job.
                ExecuteContinuationsIfExist(context);
            }
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var awaitingState = context.NewState as AwaitingState;
            if (awaitingState != null)
            {
                context.JobExpirationTimeout = awaitingState.Expiration;
            }
        }

        private void AddContinuation(ElectStateContext context, AwaitingState awaitingState)
        {
            var connection = context.Connection;
            var parentId = awaitingState.ParentId;

            // We store continuations as a json array in a job parameter. Since there 
            // is no way to add a continuation in an atomic way, we are placing a 
            // distributed lock on parent job to prevent race conditions, when
            // multiple threads add continuation to the same parent job.
            using (connection.AcquireDistributedJobLock(parentId, AddJobLockTimeout))
            {
                var continuations = GetContinuations(connection, parentId);
                continuations.Add(new Continuation { JobId = context.BackgroundJob.Id, Options = awaitingState.Options });

                var jobData = connection.GetJobData(parentId);
                if (jobData == null)
                {
                    // When we try to add a continuation for a removed job,
                    // the system should throw an exception instead of creating
                    // corrupted state.
                    throw new InvalidOperationException(
                        $"Can not add a continuation: parent background job '{parentId}' does not exist.");
                }

                var currentState = connection.GetStateData(parentId);

                // Set continuation only after ensuring that parent job still
                // exists. Otherwise we could create add non-expiring (garbage)
                // parameter for the parent job.
                SetContinuations(connection, parentId, continuations);

                if (currentState != null && _knownFinalStates.Contains(currentState.Name))
                {
                    var startImmediately = !awaitingState.Options.HasFlag(JobContinuationOptions.OnlyOnSucceededState) ||
                        currentState.Name == SucceededState.StateName;

                    context.CandidateState = startImmediately
                        ? awaitingState.NextState
                        : new DeletedState { Reason = "Continuation condition was not met" };
                }
            }
        }

        private void ExecuteContinuationsIfExist(ElectStateContext context)
        {
            // The following lines are executed inside a distributed job lock,
            // so it is safe to get continuation list here.
            var continuations = GetContinuations(context.Connection, context.BackgroundJob.Id);
            var nextStates = new Dictionary<string, IState>();

            // Getting continuation data for all continuations – state they are waiting 
            // for and their next state.
            foreach (var continuation in continuations)
            {
                if (String.IsNullOrWhiteSpace(continuation.JobId)) continue;

                var currentState = GetContinuaionState(context, continuation.JobId, ContinuationStateFetchTimeout);
                if (currentState == null)
                {
                    continue;
                }

                // All continuations should be in the awaiting state. If someone changed 
                // the state of a continuation, we should simply skip it.
                if (currentState.Name != AwaitingState.StateName) continue;

                if (continuation.Options.HasFlag(JobContinuationOptions.OnlyOnSucceededState) &&
                    context.CandidateState.Name != SucceededState.StateName)
                {
                    nextStates.Add(continuation.JobId, new DeletedState { Reason = "Continuation condition was not met" });
                    continue;
                }

                IState nextState;

                try
                {
                    nextState = JsonConvert.DeserializeObject<IState>(
                        currentState.Data["NextState"],
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });
                }
                catch (Exception ex)
                {
                    nextState = new FailedState(ex)
                    {
                        Reason = "An error occurred while deserializing the continuation"
                    };
                }

                nextStates.Add(continuation.JobId, nextState);
            }
            
            foreach (var tuple in nextStates)
            {
                _stateChanger.ChangeState(new StateChangeContext(
                    context.Storage,
                    context.Connection,
                    tuple.Key,
                    tuple.Value,
                    AwaitingState.StateName));
            }
        }

        private static StateData GetContinuaionState(ElectStateContext context, string continuationJobId, TimeSpan timeout)
        {
            StateData currentState = null;

            var started = DateTime.UtcNow;
            var firstAttempt = true;

            while (true)
            {
                var continuationData = context.Connection.GetJobData(continuationJobId);
                if (continuationData == null)
                {
                    Logger.Warn(
                        $"Can not start continuation '{continuationJobId}' for background job '{context.BackgroundJob.Id}': continuation does not exist.");

                    break;
                }

                currentState = context.Connection.GetStateData(continuationJobId);
                if (currentState != null)
                {
                    break;
                }

                if (DateTime.UtcNow >= started.Add(timeout))
                {
                    throw new TimeoutException(
                        $"Can not start continuation '{continuationJobId}' for background job '{context.BackgroundJob.Id}': timeout expired while trying to fetch continuation state.");
                }

                Thread.Sleep(firstAttempt ? 0 : 1);
                firstAttempt = false;
            }

            return currentState;
        }

        private static void SetContinuations(
            IStorageConnection connection, string jobId, List<Continuation> continuations)
        {
            connection.SetJobParameter(jobId, "Continuations", JobHelper.ToJson(continuations));
        }

        private static List<Continuation> GetContinuations(IStorageConnection connection, string jobId)
        {
            return JobHelper.FromJson<List<Continuation>>(connection.GetJobParameter(
                jobId, "Continuations")) ?? new List<Continuation>();
        }

        void IApplyStateFilter.OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }

        internal struct Continuation
        {
            public string JobId { get; set; }
            public JobContinuationOptions Options { get; set; }
        }
    }
}