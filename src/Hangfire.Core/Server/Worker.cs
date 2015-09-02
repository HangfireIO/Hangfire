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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    public class Worker : IBackgroundProcess
    {
        private static readonly TimeSpan JobInitializationWaitTimeout = TimeSpan.FromMinutes(1);
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly string[] _queues;
        private readonly IJobPerformanceProcess _performanceProcess;
        private readonly IStateChangeProcess _stateChangeProcess;
        private readonly string _workerId;

        public Worker() : this(EnqueuedState.DefaultQueue)
        {
        }

        public Worker([NotNull] params string[] queues)
            : this(queues, new JobPerformanceProcess(), new StateChangeProcess())
        {
        }

        public Worker(
            [NotNull] IEnumerable<string> queues,
            [NotNull] IJobPerformanceProcess performanceProcess, 
            [NotNull] IStateChangeProcess stateChangeProcess)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (performanceProcess == null) throw new ArgumentNullException("performanceProcess");
            if (stateChangeProcess == null) throw new ArgumentNullException("stateChangeProcess");
            
            _queues = queues.ToArray();
            _performanceProcess = performanceProcess;
            _stateChangeProcess = stateChangeProcess;
            _workerId = Guid.NewGuid().ToString();
        }

        public void Execute(BackgroundProcessContext context)
        {
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

                        var appliedState = _stateChangeProcess.ChangeState(new StateChangeContext(
                            context.Storage,
                            connection,
                            fetchedJob.JobId,
                            processingState,
                            new[] { EnqueuedState.StateName, ProcessingState.StateName },
                            linkedCts.Token));

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

                    var jobCancellationToken = new ServerJobCancellationToken(
                        connection, fetchedJob.JobId, _workerId, context.CancellationToken);

                    var state = PerformJob(fetchedJob.JobId, connection, jobCancellationToken);

                    if (state != null)
                    {
                        // Ignore return value, because we should not do anything when current state is not Processing.
                        _stateChangeProcess.ChangeState(new StateChangeContext(
                            context.Storage,
                            connection,
                            fetchedJob.JobId, 
                            state, 
                            ProcessingState.StateName));
                    }

                    // Checkpoint #4. The job was performed, and it is in the one
                    // of the explicit states (Succeeded, Scheduled and so on).
                    // It should not be re-queued, but we still need to remove its
                    // processing information.

                    fetchedJob.RemoveFromQueue();

                    // Success point. No things must be done after previous command
                    // was succeeded.
                }
                catch (JobAbortedException)
                {
                    fetchedJob.RemoveFromQueue();
                }
                catch (Exception ex)
                {
                    Logger.DebugException("An exception occurred while processing a job. It will be re-queued.", ex);

                    fetchedJob.Requeue();
                    throw;
                }
            }
        }

        public override string ToString()
        {
            return "Worker #" + _workerId.Substring(0, 8);
        }

        private IState PerformJob(string jobId, IStorageConnection connection, IJobCancellationToken token)
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
                var performContext = new PerformContext(connection, backgroundJob, token);

                var latency = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                var result = _performanceProcess.Run(performContext);
                duration.Stop();

                return new SucceededState(result, (long) latency, duration.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                throw;
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
                return new FailedState(ex)
                {
                    Reason = "An exception occurred during processing of a background job."
                };
            }
        }
    }
}