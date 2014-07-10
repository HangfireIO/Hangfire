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
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    internal class Worker : IServerComponent
    {
        private readonly WorkerContext _context;

        public Worker(WorkerContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _context.Storage.GetConnection())
            using (var fetchedJob = connection.FetchNextJob(_context.Queues, cancellationToken))
            {
                try
                {
                    ProcessJob(fetchedJob.JobId, connection, _context.PerformanceProcess, cancellationToken);

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

        private void ProcessJob(
            string jobId,
            IStorageConnection connection, 
            IJobPerformanceProcess process,
            CancellationToken shutdownToken)
        {
            var stateMachine = _context.StateMachineFactory.Create(connection);
            var processingState = new ProcessingState(_context.ServerId, _context.WorkerNumber);

            if (!stateMachine.TryToChangeState(
                jobId,
                processingState,
                new[] { EnqueuedState.StateName, ProcessingState.StateName }))
            {
                return;
            }

            // Checkpoint #3. Job is in the Processing state. However, there are
            // no guarantees that it was performed. We need to re-queue it even
            // it was performed to guarantee that it was performed AT LEAST once.
            // It will be re-queued after the JobTimeout was expired.

            IState state;

            try
            {
                var jobData = connection.GetJobData(jobId);
                jobData.EnsureLoaded();

                var cancellationToken = new ServerJobCancellationToken(
                    jobId, connection, _context, shutdownToken);

                var performContext = new PerformContext(
                    _context, connection, jobId, jobData.Job, jobData.CreatedAt, cancellationToken);

                var latency = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                process.Run(performContext, jobData.Job);
                duration.Stop();

                // TODO set proper job result
                object result = null;
                state = new SucceededState(result, (long) latency, duration.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (JobPerformanceException ex)
            {
                state = new FailedState(ex.InnerException)
                {
                    Reason = ex.Message
                };
            }
            catch (Exception ex)
            {
                state = new FailedState(ex)
                {
                    Reason = "Internal Hangfire Server exception occurred. Please, report it to Hangfire developers."
                };
            }

            // Ignore return value, because we should not do
            // anything when current state is not Processing.
            stateMachine.TryToChangeState(jobId, state, new[] { ProcessingState.StateName });
        }
    }
}