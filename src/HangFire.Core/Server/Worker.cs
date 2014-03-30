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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Common.Logging;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Server.Performing;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Server
{
    internal class Worker : IDisposable, IStoppable
    {
        private readonly JobStorage _storage;
        private readonly WorkerContext _context;

        private Thread _thread;
        private readonly ILog _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _stopSent;

        public Worker(JobStorage storage, WorkerContext context)
        {
            _storage = storage;
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
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void DoWork()
        {
            try
            {
                _logger.DebugFormat("Worker #{0} has been started.", _context.WorkerNumber);

                while (true)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    JobServer.RetryOnException(
                        () =>
                        {
                            using (var connection = _storage.GetConnection())
                            {
                                var fetcher = connection.CreateFetcher(_context.QueueNames);
                                var payload = fetcher.FetchNextJob(_cts.Token);

                                PerformJob(connection, payload);

                                // Checkpoint #4. The job was performed, and it is in the one
                                // of the explicit states (Succeeded, Scheduled and so on).
                                // It should not be re-queued, but we still need to remove its
                                // processing information.

                                connection.DeleteJobFromQueue(payload.Id, payload.Queue);

                                // Success point. No things must be done after previous command
                                // was succeeded.
                            }
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

        private void PerformJob(IStorageConnection connection, JobPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            var stateMachine = new StateMachine(connection);
            var processingState = new ProcessingState(_context.ServerName);

            if (!stateMachine.TryToChangeState(
                payload.Id, 
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
                IJobPerformStrategy performStrategy;

                var methodData = MethodData.Deserialize(payload.InvocationData);
                if (methodData.OldFormat)
                {
                    // For compatibility with the Old Client API.
                    // TODO: remove it in version 1.0
                    var arguments = JobHelper.FromJson<Dictionary<string, string>>(
                        payload.Args);

                    performStrategy = new JobAsClassPerformStrategy(
                        methodData, arguments);
                }
                else
                {
                    var arguments = JobHelper.FromJson<string[]>(payload.Arguments);

                    performStrategy = new JobAsMethodPerformStrategy(
                        methodData, arguments);
                }
                
                var performContext = new PerformContext(_context, connection, payload.Id, methodData);
                _context.PerformancePipeline.Run(performContext, performStrategy);

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
            stateMachine.TryToChangeState(payload.Id, state, new [] { ProcessingState.StateName });
        }
    }
}
