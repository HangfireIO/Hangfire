// This file is part of Hangfire.
// Copyright � 2013-2014 Sergey Odinokov.
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    /// <summary>
    /// Represents a background process responsible for <i>processing 
    /// fire-and-forget jobs</i>.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>This is the heart of background processing in Hangfire</para>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="true"/>
    /// 
    /// <seealso cref="EnqueuedState"/>
    public class Worker : IBackgroundProcess
    {
        private static readonly TimeSpan JobInitializationWaitTimeout = TimeSpan.FromMinutes(1);
        private static readonly int MaxStateChangeAttempts = 10;

        private readonly ILog _logger = LogProvider.For<Worker>();

        private readonly string _workerId;
        private readonly string[] _queues;

        private readonly IBackgroundJobPerformer _performer;
        private readonly IBackgroundJobStateChanger _stateChanger;
        
        public Worker() : this(EnqueuedState.DefaultQueue)
        {
        }

        public Worker([NotNull] params string[] queues)
            : this(queues, new BackgroundJobPerformer(), new BackgroundJobStateChanger())
        {
        }

        public Worker(
            [NotNull] IEnumerable<string> queues,
            [NotNull] IBackgroundJobPerformer performer, 
            [NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (performer == null) throw new ArgumentNullException(nameof(performer));
            if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));
            
            _queues = queues.ToArray();
            _performer = performer;
            _stateChanger = stateChanger;
            _workerId = Guid.NewGuid().ToString();
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            using (var connection = context.Storage.GetConnection())
            using (var fetchedJob = connection.FetchNextJob(_queues, context.CancellationToken))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var timeoutCts = new CancellationTokenSource(JobInitializationWaitTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        context.CancellationToken,
                        timeoutCts.Token))
                    {
                        var processingState = new ProcessingState(context.ServerId, _workerId);

                        var appliedState = TryChangeState(
                            context, 
                            connection, 
                            fetchedJob, 
                            processingState, 
                            new[] { EnqueuedState.StateName, ProcessingState.StateName },
                            linkedCts.Token);

                        // Cancel job processing if the job could not be loaded, was not in the initial state expected
                        // or if a job filter changed the state to something other than processing state
                        if (appliedState == null || !appliedState.Name.Equals(ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
                        {
                            // We should re-queue a job identifier only when graceful shutdown
                            // initiated.
                            context.CancellationToken.ThrowIfCancellationRequested();

                            // We should forget a job in a wrong state, or when timeout exceeded.
                            fetchedJob.RemoveFromQueue();
                            return;
                        }
                    }

                    // Checkpoint #3. Job is in the Processing state. However, there are
                    // no guarantees that it was performed. We need to re-queue it even
                    // it was performed to guarantee that it was performed AT LEAST once.
                    // It will be re-queued after the JobTimeout was expired.

                    var state = PerformJob(context, connection, fetchedJob.JobId);

                    if (state != null)
                    {
                        // Ignore return value, because we should not do anything when current state is not Processing.
                        TryChangeState(context, connection, fetchedJob, state, new[] { ProcessingState.StateName }, CancellationToken.None);
                    }

                    // Checkpoint #4. The job was performed, and it is in the one
                    // of the explicit states (Succeeded, Scheduled and so on).
                    // It should not be re-queued, but we still need to remove its
                    // processing information.

                    fetchedJob.RemoveFromQueue();

                    // Success point. No things must be done after previous command
                    // was succeeded.
                }
                catch (Exception ex)
                {
                    if (context.IsShutdownRequested)
                    {
                        _logger.Info(String.Format(
                            "Shutdown request requested while processing background job '{0}'. It will be re-queued.",
                            fetchedJob.JobId));
                    }
                    else
                    {
                        _logger.ErrorException("An exception occurred while processing a job. It will be re-queued.", ex);
                    }

                    Requeue(fetchedJob);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{GetType().Name} #{_workerId.Substring(0, 8)}";
        }

        private IState TryChangeState(
            BackgroundProcessContext context, 
            IStorageConnection connection, 
            IFetchedJob fetchedJob,
            IState state,
            string[] expectedStates,
            CancellationToken cancellationToken)
        {
            Exception exception = null;

            for (var retryAttempt = 0; retryAttempt < MaxStateChangeAttempts; retryAttempt++)
            {
                try
                {
                    return _stateChanger.ChangeState(new StateChangeContext(
                        context.Storage,
                        connection,
                        fetchedJob.JobId,
                        state,
                        expectedStates,
                        cancellationToken));
                }
                catch (Exception ex)
                {
                    _logger.DebugException(
                        String.Format("State change attempt {0} of {1} failed due to an error, see inner exception for details", retryAttempt+1, MaxStateChangeAttempts), 
                        ex);

                    exception = ex;
                }

                context.CancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(retryAttempt));
                context.CancellationToken.ThrowIfCancellationRequested();
            }

            return _stateChanger.ChangeState(new StateChangeContext(
                context.Storage,
                connection,
                fetchedJob.JobId,
                new FailedState(exception) { Reason = $"Failed to change state to a '{state.Name}' one due to an exception after {MaxStateChangeAttempts} retry attempts" },
                expectedStates,
                cancellationToken));
        }

        private void Requeue(IFetchedJob fetchedJob)
        {
            try
            {
                fetchedJob.Requeue();
            }
            catch (Exception ex)
            {
                _logger.WarnException($"Failed to immediately re-queue the background job '{fetchedJob.JobId}'. Next invocation may be delayed, if invisibility timeout is used", ex);
            }
        }

        private IState PerformJob(BackgroundProcessContext context, IStorageConnection connection, string jobId)
        {
            try
            {
                var jobData = connection.GetJobData(jobId);
                if (jobData == null)
                {
                    // Job expired just after moving to a processing state. This is an
                    // unreal scenario, but shit happens. Returning null instead of throwing
                    // an exception and rescuing from en-queueing a poisoned jobId back
                    // to a queue.
                    return null;
                }

                jobData.EnsureLoaded();

                var backgroundJob = new BackgroundJob(jobId, jobData.Job, jobData.CreatedAt);

                var jobToken = new ServerJobCancellationToken(connection, jobId, context.ServerId, _workerId, context.CancellationToken);
                var performContext = new PerformContext(connection, backgroundJob, jobToken);

                var latency = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                var result = _performer.Perform(performContext);
                duration.Stop();

                return new SucceededState(result, (long) latency, duration.ElapsedMilliseconds);
            }
            catch (JobAbortedException)
            {
                // Background job performance was aborted due to a
                // state change, so it's idenfifier should be removed
                // from a queue.
                return null;
            }
            catch (JobPerformanceException ex)
            {
                return new FailedState(ex.InnerException)
                {
                    Reason = ex.Message
                };
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && context.IsShutdownRequested)
                {
                    throw;
                }

                return new FailedState(ex)
                {
                    Reason = "An exception occurred during processing of a background job."
                };
            }
        }
    }
}