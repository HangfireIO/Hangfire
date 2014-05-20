// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Threading;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Server
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
                ProcessJob(fetchedJob.JobId, connection, _context.PerformanceProcess);

                // Checkpoint #4. The job was performed, and it is in the one
                // of the explicit states (Succeeded, Scheduled and so on).
                // It should not be re-queued, but we still need to remove its
                // processing information.

                fetchedJob.RemoveFromQueue();

                // Success point. No things must be done after previous command
                // was succeeded.
            }
        }

        public override string ToString()
        {
            return "Worker #" + _context.WorkerNumber;
        }

        private void ProcessJob(
            string jobId,
            IStorageConnection connection, 
            IJobPerformanceProcess process)
        {
            var stateMachine = _context.StateMachineFactory.Create(connection);
            var processingState = new ProcessingState(_context.ServerId);

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

                var performContext = new PerformContext(_context, connection, jobId, jobData.Job, jobData.CreatedAt);
                var latency = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                var duration = Stopwatch.StartNew();

                process.Run(performContext, jobData.Job);
                duration.Stop();

                state = new SucceededState((long)latency, duration.ElapsedMilliseconds);
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
                    Reason = "Internal HangFire Server exception occurred. Please, report it to HangFire developers."
                };
            }

            // Ignore return value, because we should not do
            // anything when current state is not Processing.
            stateMachine.TryToChangeState(jobId, state, new[] { ProcessingState.StateName });
        }
    }
}