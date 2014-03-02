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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HangFire.Common.States;
using HangFire.Server;
using HangFire.States;
using HangFire.Storage;

namespace HangFire
{
    public class BackgroundJobServer : IDisposable
    {
        private JobServer _server;
        private IEnumerable<string> _queues;
        private int _workerCount;
        private string _machineName;
        private TimeSpan _pollInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/>.
        /// </summary>
        public BackgroundJobServer(params string[] queues)
            : this(Environment.ProcessorCount, queues)
        {
        }

        public BackgroundJobServer(int workerCount, params string[] queues)
        {
            MachineName = Environment.MachineName;
            PollInterval = TimeSpan.FromSeconds(15);

            WorkerCount = workerCount;
            Queues = queues.Length != 0 ? queues : new[] { EnqueuedState.DefaultQueue };
        }

        public IEnumerable<string> Queues
        {
            get { return _queues; }
            set
            {
                foreach (var queue in value)
                {
                    EnqueuedState.ValidateQueueName(queue);
                }

                _queues = value;
            }
        }

        public int WorkerCount
        {
            get { return _workerCount; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value", "Worker count value must be more than zero.");
                _workerCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        public string MachineName
        {
            get { return _machineName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "Machine name value can not be null.");
                }

                if (!Regex.IsMatch(value, @"^[a-zA-Z0-9\-]+$"))
                {
                    throw new ArgumentException("Machine name must consist only of letters, digits and hyphens.");
                }

                _machineName = value;
            }
        }

        /// <summary>
        /// Gets or sets the poll interval for scheduled jobs.
        /// </summary>
        public TimeSpan PollInterval
        {
            get { return _pollInterval; }
            set
            {
                if (value != value.Duration())
                {
                    throw new ArgumentException("The poll interval value must be positive.");    
                }

                _pollInterval = value;
            }
        }

        /// <summary>
        /// Starts the server and all its workers.
        /// </summary>
        public virtual void Start()
        {
            if (_server != null)
            {
                throw new InvalidOperationException("Background job server has already been started. Please stop it first.");    
            }

            var serverName = String.Format("{0}:{1}", MachineName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

            _server = new JobServer(
                JobStorage.Current.CreateConnection(),
                serverName, WorkerCount, Queues, PollInterval, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Stops the server and its workers.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "Pair for the `Start` method",MessageId = "Stop")]
        public virtual bool Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;

                return true;
            }
            return false;
        }

        void IDisposable.Dispose()
        {
            Stop();
        }
    }
}
