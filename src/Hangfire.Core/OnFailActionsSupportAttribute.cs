using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    // POC: odinserj, please tell what do you think about such feature to run jobs on other job failure

    // i have seen few people asking about it, and I found it useful in my case too
    // maybe it's a feature for separate nuget or something but I wanted to ask you first about contribution

    // i read that your concept is that if job has failed, that means that developer needs to deploy fix and then restart the job
    // but in my scenario i have found usecases, where job has failed but we can do some automatic system reparation stuff that may bring it to life and then restart the job automatically

    // for example:
    // we are sending some email
    // var sendJob = client.Enqueue(() => SendEmail());
    // but if all retries has failed, maybe something really bad happened with the network or somehting, and we can for example restart network interfaces to fix it before some developer can look at the issue
    // se we are defining such on fail action
    // var fallback = client.OnJobFail(sendJob, () => RestartNetworkInterfacesAndDoSomeCrazyMagic());
    // after fallback action success, change state of our failed job to enqueued again to give it a try
    // client.ContinueJobWith(fallback, () => client.ChangeState(failedJobId, new EnqueuedState(), FailedState.StateName));

    // it's not tested well yet, but I wanted to post it first as a draft pull reqest to ask if you see it useful

    // i have based this on your ContinuationsSupportAttribute class

    public class OnFailActionsSupportAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
    {
        private static readonly TimeSpan AddJobLockTimeout = TimeSpan.FromMinutes(1);

        private readonly IBackgroundJobStateChanger _stateChanger;

        public OnFailActionsSupportAttribute()
            : this(new BackgroundJobStateChanger())
        {
        }

        public OnFailActionsSupportAttribute([NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));

            _stateChanger = stateChanger;

            // Ensure this filter is the last filter in the chain to start
            // on fail actions on the last candidate state only.

            // todo: think about right order
            Order = 1000;
        }

        public void OnStateElection(ElectStateContext context)
        {
            var awaitingState = context.CandidateState as AwaitingFailState;
            if (awaitingState != null)
            {
                // Branch for a child background job.
                AddOnFailActionToParentJob(context, awaitingState);
                return;
            }

            var failedState = context.CandidateState as FailedState;
            if (failedState != null)
            {
                // Branch for a parent background job.
                ExecuteOnFailActionsIfExist(context, failedState);
                return;
            }

            var succeededState = context.CandidateState as SucceededState;
            if (succeededState != null)
            {
                // Branch for a parent background job.
                DeleteOnFailActionsIfExist(context);
                return;
            }
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            // TODO: Remove this method and IApplyStateFilter interface in 2.0.0.
        }

        private void AddOnFailActionToParentJob(ElectStateContext context, AwaitingFailState awaitingState)
        {
            var connection = context.Connection;
            var parentId = awaitingState.ParentId;

            // We store on fail actions as a json array in a job parameter. Since there 
            // is no way to add action in an atomic way, we are placing a 
            // distributed lock on parent job to prevent race conditions, when
            // multiple threads add action to the same parent job.
            using (connection.AcquireDistributedJobLock(parentId, AddJobLockTimeout))
            {
                var jobData = connection.GetJobData(parentId);
                if (jobData == null)
                {
                    // When we try to add a on fail action for a removed job,
                    // the system should throw an exception instead of creating
                    // corrupted state.
                    throw new InvalidOperationException(
                        $"Can not add a on fail action: parent background job '{parentId}' does not exist.");
                }

                var actions = GetOnFailActions(connection, parentId);

                // On fail actions may be already added. This may happen, when outer transaction
                // was failed after adding a action last time, since the addition is
                // performed outside of an outer transaction.
                if (!actions.Exists(x => x.JobId == context.BackgroundJob.Id))
                {
                    actions.Add(new OnFailAction { JobId = context.BackgroundJob.Id });

                    // Set on fail action only after ensuring that parent job still
                    // exists. Otherwise we could create add non-expiring (garbage)
                    // parameter for the parent job.
                    SetOnFailActions(connection, parentId, actions);
                }

                var currentState = connection.GetStateData(parentId);

                // If parent job is already in failed state, get its exception data and start on fail action.
                if (currentState != null && currentState.Name == FailedState.StateName)
                {
                    if (currentState.Data?.TryGetValue("ExceptionType", out string type) == true)
                    {
                        connection.SetJobParameter(awaitingState.ParentId, "ExceptionType", type);
                    }

                    if (currentState.Data?.TryGetValue("ExceptionMessage", out string message) == true)
                    {
                        connection.SetJobParameter(awaitingState.ParentId, "ExceptionMessage", message);
                    }

                    if (currentState.Data?.TryGetValue("ExceptionDetails", out string details) == true)
                    {
                        connection.SetJobParameter(awaitingState.ParentId, "ExceptionDetails", details);
                    }

                    context.CandidateState = new EnqueuedState();
                    return;
                }

                // If parent job is succeeded or deleted, delete on fail action.
                if (currentState.Name == SucceededState.StateName || currentState.Name == DeletedState.StateName)
                {
                    context.CandidateState = new DeletedState();
                }
            }
        }

        private void ExecuteOnFailActionsIfExist(ElectStateContext context, FailedState failedState)
        {
            // The following lines are executed inside a distributed job lock,
            // so it is safe to get on fail actions list here.
            var onFailActions = GetOnFailActions(context.Connection, context.BackgroundJob.Id);

            // Getting on fail actions data for all actions – state they are waiting 
            // for and their next state.
            foreach (var action in onFailActions)
            {
                if (String.IsNullOrWhiteSpace(action.JobId)) continue;

                // pass exception details from parent
                if (failedState.Exception != null)
                {
                    context.Connection.SetJobParameter(action.JobId, "ExceptionType", failedState.Exception.GetType().FullName);
                    context.Connection.SetJobParameter(action.JobId, "ExceptionMessage", failedState.Exception.Message);

                    // todo: Exception.ToStringWithOriginalStackTrace(MaxLinesInExceptionDetails) ?
                    context.Connection.SetJobParameter(action.JobId, "ExceptionDetails", failedState.Exception.ToString());
                }

                _stateChanger.ChangeState(new StateChangeContext(
                   context.Storage,
                   context.Connection,
                   action.JobId,
                   new EnqueuedState()));
            }
        }

        private void DeleteOnFailActionsIfExist(ElectStateContext context)
        {
            // The following lines are executed inside a distributed job lock,
            // so it is safe to get on fail actions list here.
            var onFailActions = GetOnFailActions(context.Connection, context.BackgroundJob.Id);

            // Getting on fail actions data for all actions – state they are waiting 
            // for and their next state.
            foreach (var action in onFailActions)
            {
                if (String.IsNullOrWhiteSpace(action.JobId)) continue;

                _stateChanger.ChangeState(new StateChangeContext(
                   context.Storage,
                   context.Connection,
                   action.JobId,
                   new DeletedState()));
            }
        }

        private static void SetOnFailActions(IStorageConnection connection, string jobId, List<OnFailAction> actions)
        {
            connection.SetJobParameter(jobId, "OnFailActions", SerializationHelper.Serialize(actions));
        }

        private static List<OnFailAction> GetOnFailActions(IStorageConnection connection, string jobId)
        {
            var parameter = connection.GetJobParameter(jobId, "OnFailActions");
            var deserialized = SerializationHelper.Deserialize<List<OnFailAction>>(parameter);
            return deserialized ?? new List<OnFailAction>();
        }

        void IApplyStateFilter.OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }

    public struct OnFailAction
    {
        public string JobId;
    }

    public class AwaitingFailState : IState
    {
        public string Name => "AwaitingFail";

        public string Reason { get; set; }

        public bool IsFinal => false;

        public bool IgnoreJobLoadException => false;

        public string ParentId { get; set; }

        public Dictionary<string, string> SerializeData() => new Dictionary<string, string> { { "ParentId", ParentId } };
    }

    public static class Extensions
    {
        /// <summary>
        /// Creates a new background job that will wait for another background job to fail.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string OnJobFail(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingFailState() { ParentId = parentId };
            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to fail.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string OnJobFail(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingFailState() { ParentId = parentId };
            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to fail.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string OnJobFail<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingFailState() { ParentId = parentId };
            return client.Create(Job.FromExpression(methodCall), state);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to fail.
        /// </summary>
        /// <param name="client">A job client instance.</param>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string OnJobFail<T>(
            [NotNull] this IBackgroundJobClient client,
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var state = new AwaitingFailState() { ParentId = parentId };
            return client.Create(Job.FromExpression(methodCall), state);
        }


        // todo add BackgroundJob.OnJobFail static functions
    }
}
