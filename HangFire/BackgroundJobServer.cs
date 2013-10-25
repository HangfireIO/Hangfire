using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HangFire.Server;

namespace HangFire
{
    public class BackgroundJobServer : IDisposable
    {
        private JobServer _server;

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
            Queues = queues ?? new[] { "default" };
        }

        public IEnumerable<string> Queues { get; private set; } 

        public int WorkerCount { get; private set; }

        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the poll interval for scheduled jobs.
        /// </summary>
        public TimeSpan PollInterval { get; set; }

        /// <summary>
        /// Get or sets an instance of the <see cref="HangFire.JobActivator"/> class
        /// that will be used to instantinate jobs.
        /// </summary>
        public JobActivator JobActivator { get; set; }

        /// <summary>
        /// Starts the server and all its workers.
        /// </summary>
        public virtual void Start()
        {
            if (_server != null)
            {
                throw new InvalidOperationException("Background job server has already been started. Please stop it first.");    
            }

            var serverName = String.Format("{0}:{1}", MachineName, Process.GetCurrentProcess().Id);

            _server = new JobServer(
                RedisFactory.BasicManager,
                serverName, WorkerCount, Queues, JobActivator, PollInterval, TimeSpan.FromSeconds(5));
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
