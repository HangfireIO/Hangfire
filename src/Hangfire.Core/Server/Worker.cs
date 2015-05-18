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
using System.Diagnostics;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    internal class Worker : IServerComponent
    {
        private static readonly TimeSpan JobInitializationWaitTimeout = SchedulerResolution.Current.GetJobInitializationWaitTimeout();

        private readonly JobStorage _storage;
        private readonly IJobPerformanceProcess _process;
        private readonly IStateMachineFactory _stateMachineFactory;
        private readonly WorkerContext _context;

        public Worker(
            [NotNull] WorkerContext context,
            [NotNull] JobStorage storage, 
            [NotNull] IJobPerformanceProcess process, 
            [NotNull] IStateMachineFactory stateMachineFactory)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (storage == null) throw new ArgumentNullException("storage");
            if (process == null) throw new ArgumentNullException("process");
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");
            
            _context = context;
            _storage = storage;
            _process = process;
            _stateMachineFactory = stateMachineFactory;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            using (var fetchedJob = connection.FetchNextJob(_context.Queues, cancellationToken))
            {
                try
                {
                    var stateMachine = _stateMachineFactory.Create(connection);

                    using (var timeoutCts = new CancellationTokenSource(JobInitializationWaitTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutCts.Token))
                    {
                        var processingState = new ProcessingState(_context.ServerId, _context.WorkerNumber);

                        if (!stateMachine.ChangeState(
                            fetchedJob.JobId,
                            processingState,
                            new[] { EnqueuedState.StateName, ProcessingState.StateName },
                            linkedCts.Token))
                        {
                            // We should re-queue a job identifier only when graceful shutdown
                            // initiated.
                            cancellationToken.ThrowIfCancellationRequested();

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
                        fetchedJob.JobId, connection, _context, cancellationToken);

                    var state = PerformJob(fetchedJob.JobId, connection, jobCancellationToken);

                    if (state != null)
                    {
                        // Ignore return value, because we should not do anything when current state is not Processing.
                        stateMachine.ChangeState(fetchedJob.JobId, state, new[] { ProcessingState.StateName });
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
                catch (Exception)
                {
                    fetchedJob.Requeue();
                    throw;
                }
            }
        }

        public override string ToString()
        {
            return "Worker #" + _context.WorkerNumber;
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

                var performContext = new PerformContext(
                    _context, connection, jobId, jobData.Job, jobData.CreatedAt, token);

                var latency = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                var result = _process.Run(performContext, jobData.Job);
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
                    Reason = "Internal Hangfire Server exception occurred. Please, report it to Hangfire developers."
                };
            }
        }
    }
}