// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Server.Performing;
using HangFire.States;
using HangFire.Storage;
using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class Worker : IDisposable, IStoppable
    {
        private readonly WorkerContext _context;
        private readonly StateMachine _stateMachine;
        private readonly IStorageConnection _connection;

        private Thread _thread;
        private readonly ILog _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _stopSent;

        public Worker(WorkerContext context)
        {
            _connection = JobStorage.Current.CreateConnection();
            _stateMachine = new StateMachine(_connection);

            _context = context;

            _logger = LogManager.GetLogger(String.Format("HangFire.Worker.{0}", context.WorkerNumber));
        }

        public void Start()
        {
            _thread = new Thread(DoWork)
            {
                Name = String.Format("HangFire.Worker.{0}", _context.WorkerNumber),
                IsBackground = true
            };
            _thread.Start();
        }

        public void Stop()
        {
            _stopSent = true;
            _cts.Cancel();
        }

        public void Dispose()
        {
            if (!_stopSent)
            {
                Stop();
            }

            if (_thread != null)
            {
                _thread.Join();
                _thread = null;
            }

            _cts.Dispose();

            _connection.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void DoWork()
        {
            try
            {
                _logger.DebugFormat("Worker #{0} has been started.", _context.WorkerNumber);

                var fetcher = _connection.CreateFetcher(_context.QueueNames);
                while (true)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    JobServer.RetryOnException(
                        () =>
                        {
                            var job = fetcher.DequeueJob(_cts.Token);

                            if (job == null)
                            {
                                _cts.Cancel();
                                return;
                            }

                            PerformJob(job.Payload);

                            // Checkpoint #4. The job was performed, and it is in the one
                            // of the explicit states (Succeeded, Scheduled and so on).
                            // It should not be re-queued, but we still need to remove its
                            // processing information.

                            job.Complete(_connection);

                            // Success point. No things must be done after previous command
                            // was succeeded.
                        }, _cts.Token.WaitHandle);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.DebugFormat("Worker #{0} has been stopped.", _context.WorkerNumber);
            }
            catch (Exception ex)
            {
                _logger.Fatal(
                    String.Format("Unexpected exception caught. The worker will be stopped."),
                    ex);
            }
        }

        private void PerformJob(JobPayload payload)
        {
            if (payload.Job.Values.All(x => x == null))
            {
                return;
            }

            var processingState = new ProcessingState("Worker has started processing.", _context.ServerName);
            if (!_stateMachine.ChangeState(payload.Id, processingState, EnqueuedState.Name))
            {
                return;
            }

            // Checkpoint #3. Job is in the Processing state. However, there are
            // no guarantees that it was performed. We need to re-queue it even
            // it was performed to guarantee that it was performed AT LEAST once.
            // It will be re-queued after the JobTimeout was expired.

            JobState state;

            try
            {
                IJobPerformStrategy performStrategy;

                var jobMethod = JobMethod.Deserialize(payload.Job);
                if (jobMethod.OldFormat)
                {
                    // For compatibility with the Old Client API.
                    // TODO: remove it in version 1.0
                    var arguments = JobHelper.FromJson<Dictionary<string, string>>(
                        payload.Job["Args"]);

                    performStrategy = new JobAsClassPerformStrategy(
                        jobMethod, arguments);
                }
                else
                {
                    var arguments = JobHelper.FromJson<string[]>(payload.Job["Arguments"]);

                    performStrategy = new JobAsMethodPerformStrategy(
                        jobMethod, arguments);
                }

                var performContext = new PerformContext(_context, _connection, payload.Id, jobMethod);
                _context.Performer.PerformJob(performContext, performStrategy);

                state = new SucceededState("The job has been completed successfully.");
            }
            catch (Exception ex)
            {
                state = new FailedState("The job has been failed.", ex);
            }

            _stateMachine.ChangeState(payload.Id, state, ProcessingState.Name);
        }
    }
}
