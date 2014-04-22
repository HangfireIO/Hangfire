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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Common.Logging;
using HangFire.Server.Performing;

namespace HangFire.Server
{
    internal class Worker : IDisposable, IStoppable
    {
        private readonly JobStorage _storage;
        private readonly WorkerContext _context;
        private readonly IJobPerformanceProcess _process;

        private Thread _thread;
        private readonly ILog _logger;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _stopSent;

        public Worker(JobStorage storage, WorkerContext context)
            : this(storage, context, new JobPerformanceProcess())
        {
        }

        public Worker(JobStorage storage, WorkerContext context, IJobPerformanceProcess process)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (context == null) throw new ArgumentNullException("context");
            if (process == null) throw new ArgumentNullException("process");

            _storage = storage;
            _context = context;
            _process = process;

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

        private void ProcessNextJob()
        {
            using (var connection = _storage.GetConnection())
            using (var nextJob = connection.FetchNextJob(_context.QueueNames, _cts.Token))
            {
                nextJob.Process(_context, _process);

                // Checkpoint #4. The job was performed, and it is in the one
                // of the explicit states (Succeeded, Scheduled and so on).
                // It should not be re-queued, but we still need to remove its
                // processing information.
            }

            // Success point. No things must be done after previous command
            // was succeeded.
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
                        ProcessNextJob, 
                        _cts.Token.WaitHandle);
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
    }
}
