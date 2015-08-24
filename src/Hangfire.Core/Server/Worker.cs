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
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    public class Worker : IBackgroundProcess
    {
        private static readonly TimeSpan JobInitializationWaitTimeout = TimeSpan.FromMinutes(1);
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly IJobPerformanceProcess _process;
        private readonly IStateMachineFactoryFactory _stateMachineFactoryFactory;
        private readonly WorkerContext _context;

        public Worker([NotNull] WorkerContext context)
            : this(context, new DefaultJobPerformanceProcess())
        {
        }

        public Worker([NotNull] WorkerContext context, [NotNull] IJobPerformanceProcess process)
            : this(context, process, new StateMachineFactoryFactory())
        {
        }

        public Worker(
            [NotNull] WorkerContext context,
            [NotNull] IJobPerformanceProcess process, 
            [NotNull] IStateMachineFactoryFactory stateMachineFactoryFactory)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (process == null) throw new ArgumentNullException("process");
            if (stateMachineFactoryFactory == null) throw new ArgumentNullException("stateMachineFactoryFactory");
            
            _context = context;
            _process = process;
            _stateMachineFactoryFactory = stateMachineFactoryFactory;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var stateMachineFactory = _stateMachineFactoryFactory.CreateFactory(context.Storage);

            using (var connection = context.Storage.GetConnection())
            using (var fetchedJob = connection.FetchNextJob(_context.Queues, context.CancellationToken))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var stateMachine = stateMachineFactory.Create(connection);

                    using (var timeoutCts = new CancellationTokenSource(JobInitializationWaitTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        context.CancellationToken,
                        timeoutCts.Token))
                    {
                        var processingState = new ProcessingState(context.ServerId, _context.WorkerNumber);

                        var appliedState = stateMachine.ChangeState(new StateChangeContext(
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
                        fetchedJob.JobId, connection, _context, context);

                    var state = PerformJob(fetchedJob.JobId, connection, jobCancellationToken);

                    if (state != null)
                    {
                        // Ignore return value, because we should not do anything when current state is not Processing.
                        stateMachine.ChangeState(new StateChangeContext(
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

                var backgroundJob = new BackgroundJob(jobId, jobData.Job, jobData.CreatedAt);
                var performContext = new PerformContext(_context, connection, backgroundJob, token);

                var latency = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                var result = _process.Run(performContext);
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