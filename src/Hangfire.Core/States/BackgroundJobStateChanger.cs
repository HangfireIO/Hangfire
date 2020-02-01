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
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class BackgroundJobStateChanger : IBackgroundJobStateChanger
    {
        private static readonly TimeSpan JobLockTimeout = TimeSpan.FromMinutes(15);

        private readonly IStateMachine _stateMachine;

        public BackgroundJobStateChanger()
            : this(JobFilterProviders.Providers)
        {
        }

        public BackgroundJobStateChanger([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, new CoreStateMachine())
        {
        }

        internal BackgroundJobStateChanger([NotNull] IJobFilterProvider filterProvider, [NotNull] IStateMachine stateMachine)
        {
            if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));

            _stateMachine = new StateMachine(filterProvider, stateMachine);
        }
        
        public IState ChangeState(StateChangeContext context)
        {
            // To ensure that job state will be changed only from one of the
            // specified states, we need to ensure that other users/workers
            // are not able to change the state of the job during the
            // execution of this method. To guarantee this behavior, we are
            // using distributed application locks and rely on fact, that
            // any state transitions will be made only within a such lock.
            using (context.Connection.AcquireDistributedJobLock(context.BackgroundJobId, JobLockTimeout))
            {
                var jobData = GetJobData(context);

                if (jobData == null)
                {
                    return null;
                }

                if (context.ExpectedStates != null && !context.ExpectedStates.Contains(jobData.State, StringComparer.OrdinalIgnoreCase))
                {
                    return null;
                }
                
                var stateToApply = context.NewState;

                try
                {
                    jobData.EnsureLoaded();
                }
                catch (JobLoadException ex)
                {
                    // This happens when Hangfire couldn't find the target method,
                    // serialized within a background job. There are many reasons
                    // for this case, including refactored code, or a missing
                    // assembly reference due to a mistake or erroneous deployment.
                    // 
                    // The problem is that in this case we can't get any filters,
                    // applied at a method or a class level, and we can't proceed
                    // with the state change without breaking a consistent behavior:
                    // in some cases our filters will be applied, and in other ones
                    // will not.

                    // TODO 1.X/2.0:
                    // There's a problem with filters related to handling the states
                    // which ignore this exception, i.e. fitlers for the FailedState
                    // and the DeletedState, such as AutomaticRetryAttrubute filter.
                    // 
                    // We should document that such a filters may not be fired, when
                    // we can't find a target method, and these filters should be
                    // applied only at the global level to get consistent results.
                    // 
                    // In 2.0 we should have a special state for all the errors, when
                    // Hangfire doesn't know what to do, without any possibility to
                    // add method or class-level filters for such a state to provide
                    // the same behavior no matter what.

                    if (!stateToApply.IgnoreJobLoadException)
                    {
                        stateToApply = new FailedState(ex.InnerException)
                        {
                            Reason = $"Can not change the state to '{stateToApply.Name}': target method was not found."
                        };
                    }
                }

                using (var transaction = context.Connection.CreateWriteTransaction())
                {
                    var applyContext = new ApplyStateContext(
                        context.Storage,
                        context.Connection,
                        transaction,
                        new BackgroundJob(context.BackgroundJobId, jobData.Job, jobData.CreatedAt),
                        stateToApply,
                        jobData.State,
                        context.Profiler);

                    var appliedState = _stateMachine.ApplyState(applyContext);

                    transaction.Commit();

                    return appliedState;
                }
            }
        }

        private static JobData GetJobData(StateChangeContext context)
        {
            // This code was introduced as a fix for an issue, which appeared when an
            // external queue implementation was used together with a non-linearizable
            // storage. The problem was likely related to the SQL Azure + Azure ServiceBus
            // (or RabbitMQ or so) bundle, because the READCOMMITTED_SNAPSHOT_ON setting
            // is enabled by default there.
            //
            // Since external queueing doesn't share the linearization point with the
            // storage, it is possible that a worker will pick up a background job before
            // its transaction was committed. Non-linearizable read will simply return
            // the NULL value instead of waiting for a transaction to be committed. With
            // this code, we will make several retry attempts to handle this case to wait
            // on the client side.
            // 
            // On the other hand, we need to give up after some retry attempt, because
            // we should also handle the case, when our queue and job storage became
            // unsynchronized with each other due to failures, manual intervention or so.
            // Otherwise we will wait forever in this cases, since And there's no way to
            // make a distinction between a non-linearizable read and the storages, non-
            // synchronized with each other.
            // 
            // In recent versions, Hangfire.SqlServer uses query hints to make all the
            // reads linearizable no matter what, but there may be other storages that
            // still require this workaround.

            // TODO 2.0:
            // Eliminate the need of this timeout by placing an explicit requirement to
            // storage implementations to either have a single linearization point for all
            // the operations inside a transaction; or make all the reads linearizable and
            // execute queueing operations after all the other ones in a transaction.

            var firstAttempt = true;

            while (true)
            {
                var jobData = context.Connection.GetJobData(context.BackgroundJobId);

                // Empty state means our job wasn't moved to any state after its creation.
                // Such a jobs may be created by internal logic, and those jobs have very
                // special meaning, thus we shouldn't allow state changer to alter them,
                // using this class (which can be used by users), leaving this logic to
                // low level API only, i.e. state machine.

                // TODO 1.X:
                // However, we shouldn't wait for the initial state change, because in some
                // cases (like in batches) it may take days. We should throw an exception
                // instead, clearly indicating that such a state change is prohibited. There
                // may be some issues on GitHub, related to the hanging dashboard requests
                // in this case.

                if (!String.IsNullOrEmpty(jobData?.State) || context.Storage.LinearizableReads)
                {
                    return jobData;
                }

                // State change can also be requested from user's request processing logic.
                // There is always a chance it will be issued against a non-existing or an
                // already expired background job, and a minute wait (or whatever timeout is
                // used) is completely unnecessary in this case.
                // 
                // Since waiting is only required when a worker picks up a job, and 
                // cancellation tokens are used only by the Worker class, we can avoid the
                // unnecessary waiting logic when no cancellation token is passed.

                if (context.CancellationToken.IsCancellationRequested ||
                    context.CancellationToken == CancellationToken.None)
                {
                    return null;
                }

                context.CancellationToken.Wait(TimeSpan.FromMilliseconds(firstAttempt ? 0 : 100));
                firstAttempt = false;
            }
        }
    }
}
