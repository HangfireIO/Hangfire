// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public class BackgroundJobServerOptions
    {
        // https://github.com/HangfireIO/Hangfire/issues/246
        private const int MaxDefaultWorkerCount = 20;

        private int _workerCount;
        private string[] _queues;
        private TimeSpan _serverTimeout;
        private TimeSpan _serverCheckInterval;
        private TimeSpan _heartbeatInterval;
        private TimeSpan _stopTimeout;
        private TimeSpan _shutdownTimeout;
        private TimeSpan _schedulePollingInterval;

        public BackgroundJobServerOptions()
        {
            WorkerCount = Math.Min(Environment.ProcessorCount * 5, MaxDefaultWorkerCount);
            Queues = new[] { EnqueuedState.DefaultQueue };
            StopTimeout = BackgroundProcessingServerOptions.DefaultStopTimeout;
            ShutdownTimeout = BackgroundProcessingServer.DefaultShutdownTimeout;
            SchedulePollingInterval = DelayedJobScheduler.DefaultPollingDelay;
            HeartbeatInterval = BackgroundProcessingServerOptions.DefaultHeartbeatInterval;
            ServerTimeout = ServerWatchdog.DefaultServerTimeout;
            ServerCheckInterval = ServerWatchdog.DefaultCheckInterval;
            CancellationCheckInterval = ServerJobCancellationWatcher.DefaultCheckInterval;
            
            FilterProvider = null;
            Activator = null;
            TimeZoneResolver = null;
            TaskScheduler = TaskScheduler.Default;
        }
        
        public string ServerName { get; set; }

        public int WorkerCount
        {
            get { return _workerCount; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "WorkerCount property value should be positive.");

                _workerCount = value;
            }
        }

        public string[] Queues
        {
            get { return _queues; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length == 0) throw new ArgumentException("You should specify at least one queue to listen.", nameof(value));

                _queues = value;
            }
        }

        public TimeSpan StopTimeout
        {
            get => _stopTimeout;
            set
            {
                if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) || value.TotalMilliseconds > Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"StopTimeout must be either equal to or less than {Int32.MaxValue} milliseconds and non-negative or infinite");
                }
                _stopTimeout = value;
            }
        }

        public TimeSpan ShutdownTimeout
        {
            get { return _shutdownTimeout; }
            set
            {
                if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) || value.TotalMilliseconds > Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ShutdownTimeout must be either equal to or less than {Int32.MaxValue} milliseconds and non-negative or infinite");
                }
                _shutdownTimeout = value;
            }
        }

        public TimeSpan SchedulePollingInterval
        {
            get { return _schedulePollingInterval; }
            set
            {
                if (value < TimeSpan.Zero || value.TotalMilliseconds > Int32.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"SchedulePollingInterval must be non-negative and either equal to or less than {Int32.MaxValue} milliseconds");
                }

                _schedulePollingInterval = value;
            }
        }

        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxHeartbeatInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"HeartbeatInterval must be either non-negative and equal to or less than {ServerWatchdog.MaxHeartbeatInterval.Hours} hours");
                }
                _heartbeatInterval = value;
            }
        }

        public TimeSpan ServerCheckInterval
        {
            get { return _serverCheckInterval; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerCheckInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ServerCheckInterval must be either non-negative and equal to or less than {ServerWatchdog.MaxServerCheckInterval.Hours} hours");
                }
                _serverCheckInterval = value;
            }
        }

        public TimeSpan ServerTimeout
        {
            get { return _serverTimeout; }
            set
            {
                if (value < TimeSpan.Zero || value > ServerWatchdog.MaxServerTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"ServerTimeout must be either non-negative and equal to or less than {ServerWatchdog.MaxServerTimeout.Hours} hours");
                }

                _serverTimeout = value;

            }
        }

        public TimeSpan CancellationCheckInterval { get; set; }

        [Obsolete("Please use `ServerTimeout` or `ServerCheckInterval` options instead. Will be removed in 2.0.0.")]
        public ServerWatchdogOptions ServerWatchdogOptions { get; set; }

        [CanBeNull]
        public IJobFilterProvider FilterProvider { get; set; }

        [CanBeNull]
        public JobActivator Activator { get; set; }

        [CanBeNull]
        public ITimeZoneResolver TimeZoneResolver { get; set; }

        [CanBeNull]
        public TaskScheduler TaskScheduler { get; set; }
    }
}