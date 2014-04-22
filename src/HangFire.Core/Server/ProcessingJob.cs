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
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Server.Performing;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Server
{
    public class ProcessingJob : IDisposable
    {
        private readonly IStorageConnection _connection;

        public ProcessingJob(
            IStorageConnection connection,
            string id,
            string queue)
        {
            _connection = connection;
            Id = id;
            Queue = queue;
        }

        public string Id { get; private set; }
        public string Queue { get; private set; }

        internal virtual void Process(WorkerContext context, IJobPerformanceProcess process)
        {
            var stateMachine = _connection.CreateStateMachine();
            var processingState = new ProcessingState(context.ServerName);

            if (!stateMachine.TryToChangeState(
                Id, 
                processingState, 
                new [] { EnqueuedState.StateName, ProcessingState.StateName }))
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
                IJobPerformer performer;

                var jobData = _connection.GetJobData(Id);
                jobData.EnsureLoaded();

                if (jobData.MethodData.OldFormat)
                {
                    performer = new JobAsClassPerformer(jobData.MethodData, jobData.Args);
                }
                else
                {
                    performer = new Job(jobData.MethodData, jobData.Arguments);
                }
                
                var performContext = new PerformContext(context, _connection, Id, jobData.MethodData);
                process.Run(performContext, performer);

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

            // TODO: check return value
            stateMachine.TryToChangeState(Id, state, new [] { ProcessingState.StateName });
        }

        public void Dispose()
        {
            _connection.DeleteJobFromQueue(Id, Queue);
        }
    }
}
