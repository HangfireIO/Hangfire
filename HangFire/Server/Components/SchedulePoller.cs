// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using ServiceStack.Logging;

namespace HangFire.Server.Components
{
    public class SchedulePoller : IThreadWrappable, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SchedulePoller));

        private readonly IStorageConnection _connection;
        private readonly TimeSpan _pollInterval;
        private readonly StateMachine _stateMachine;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SchedulePoller(IStorageConnection connection, TimeSpan pollInterval)
        {
            _connection = connection;
            _stateMachine = new StateMachine(_connection);
            _pollInterval = pollInterval;
        }

        public bool EnqueueNextScheduledJob()
        {
            var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

            // TODO: it is very slow. Add batching.
            var jobId = _connection.Sets
                .GetFirstByLowestScore("schedule", 0, timestamp);

            if (String.IsNullOrEmpty(jobId))
            {
                return false;
            }

            var enqueuedState = new EnqueuedState("Enqueued by the schedule poller");
            _stateMachine.ChangeState(jobId, enqueuedState, ScheduledState.Name);

            return true;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        void IThreadWrappable.Work()
        {
            try
            {
                Logger.Info("Schedule poller has been started.");

                int enqueued = 0;

                while (true)
                {
                    var wasEnqueued = false;

                    JobServer.RetryOnException(
                        () =>
                        {
                            wasEnqueued = EnqueueNextScheduledJob();
                        }, _cts.Token.WaitHandle);

                    if (wasEnqueued && !_cts.IsCancellationRequested)
                    {
                        enqueued++;
                        continue;
                    }

                    if (enqueued != 0)
                    {
                        Logger.InfoFormat("{0} scheduled jobs were enqueued.", enqueued);
                        enqueued = 0;
                    }

                    if (_cts.Token.WaitHandle.WaitOne(_pollInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Schedule poller has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal(
                    "Unexpected exception caught in the schedule poller. Scheduled jobs will not be added to their queues.",
                    ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _cts.Cancel();
            thread.Join();
        }
    }
}
