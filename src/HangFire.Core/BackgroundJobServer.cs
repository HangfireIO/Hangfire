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
using System.Diagnostics.CodeAnalysis;
using HangFire.Server;
using HangFire.States;

namespace HangFire
{
    public class BackgroundJobServer : IDisposable
    {
        private static readonly int DefaultWorkerCount
            = Environment.ProcessorCount * 5;

        private JobServer _server;
        private string _machineName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/>.
        /// </summary>
        public BackgroundJobServer(params string[] queues)
            : this(DefaultWorkerCount, queues)
        {
        }

        public BackgroundJobServer(int workerCount, params string[] queues)
        {
            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");

            MachineName = Environment.MachineName;

            WorkerCount = workerCount;
            Queues = queues.Length != 0 ? queues : new[] { EnqueuedState.DefaultQueue };
        }

        public string[] Queues { get; private set; }
        public int WorkerCount { get; private set; }

        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        public string MachineName
        {
            get { return _machineName; }
            private set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "Machine name value can not be null.");
                }

                _machineName = value;
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

            _server = new JobServer(JobStorage.Current, serverName, WorkerCount, Queues);
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
