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
using System.Linq;
using Hangfire.Client;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class BackgroundJobServerOptions
    {
        // https://github.com/HangfireIO/Hangfire/issues/246
        private const int MaxDefaultWorkerCount = 40;

        private int _workerCount;
        private string[] _queues;

        public BackgroundJobServerOptions()
        {
            WorkerCount = Math.Min(Environment.ProcessorCount * 5, MaxDefaultWorkerCount);
            Queues = new[] { EnqueuedState.DefaultQueue };
            ShutdownTimeout = BackgroundServer.DefaultShutdownTimeout;
            SchedulePollingInterval = SchedulePoller.DefaultPollingInterval;
            HeartbeatInterval = ServerHeartbeat.DefaultHeartbeatInterval;

            ServerWatchdogOptions = new ServerWatchdogOptions();

            CreationProcess = new DefaultJobCreationProcess();
            PerformanceProcess = new DefaultJobPerformanceProcess();
            StateChangeProcess = new StateChangeProcess();
        }

        public string ServerName { get; set; }

        public int WorkerCount
        {
            get { return _workerCount; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value", "WorkerCount property value should be positive.");

                _workerCount = value;
            }
        }

        public string[] Queues
        {
            get { return _queues; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value.Length == 0) throw new ArgumentException("You should specify at least one queue to listen.", "value");

                _queues = value;
            }
        }

        public TimeSpan ShutdownTimeout { get; set; }
        public TimeSpan SchedulePollingInterval { get; set; }
        public TimeSpan HeartbeatInterval { get; set; }

        public ServerWatchdogOptions ServerWatchdogOptions { get; set; }

        public IJobCreationProcess CreationProcess { get; set; }
        public IJobPerformanceProcess PerformanceProcess { get; set; }
        public IStateChangeProcess StateChangeProcess { get; set; }

        public void WriteToLog(ILog logger)
        {
            logger.InfoFormat("Using the following options for Hangfire Server:");
            logger.InfoFormat("    Worker count: {0}.", WorkerCount);
            logger.InfoFormat("    Listening queues: {0}.", String.Join(", ", Queues.Select(x => "'" + x + "'")));
            logger.InfoFormat("    Shutdown timeout: {0}.", ShutdownTimeout);
            logger.InfoFormat("    Schedule polling interval: {0}.", SchedulePollingInterval);
        }
    }
}