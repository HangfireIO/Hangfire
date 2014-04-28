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
using System.Threading;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Server
{
    internal class Worker : IServerComponent
    {
        private readonly JobStorage _storage;
        private readonly WorkerContext _context;
        private readonly IJobPerformanceProcess _process;
        private readonly IStateMachineFactory _stateMachineFactory;

        public Worker(
            WorkerContext context,
            JobStorage storage,  
            IJobPerformanceProcess process,
            IStateMachineFactory stateMachineFactory)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (context == null) throw new ArgumentNullException("context");
            if (process == null) throw new ArgumentNullException("process");
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");

            _storage = storage;
            _context = context;
            _process = process;
            _stateMachineFactory = stateMachineFactory;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                var nextJob = connection.FetchNextJob(_context.QueueNames, cancellationToken);

                try
                {
                    ProcessJob(nextJob.JobId, connection, _process);

                    // Checkpoint #4. The job was performed, and it is in the one
                    // of the explicit states (Succeeded, Scheduled and so on).
                    // It should not be re-queued, but we still need to remove its
                    // processing information.
                }
                finally
                {
                    connection.DeleteJobFromQueue(nextJob.JobId, nextJob.Queue);

                    // Success point. No things must be done after previous command
                    // was succeeded.
                }
            }
        }

        private void ProcessJob(
            string jobId,
            IStorageConnection connection, 
            IJobPerformanceProcess process)
        {
            var stateMachine = _stateMachineFactory.Create(connection);
            var processingState = new ProcessingState(_context.ServerName);

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

            State state;

            try
            {
                var jobData = connection.GetJobData(jobId);
                jobData.EnsureLoaded();

                var performContext = new PerformContext(_context, connection, jobId, jobData.Job);

                process.Run(performContext, jobData.Job);

                state = new SucceededState();
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