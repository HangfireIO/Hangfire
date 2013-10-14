using System;
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
        public BackgroundJobServer()
        {
            MachineName = Environment.MachineName;
            WorkersCount = Environment.ProcessorCount * 2;
            Queue = "default";
            PollInterval = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the queue to listen.
        /// </summary>
        public string Queue { get; set; }

        /// <summary>
        /// Gets or sets maximum amount of jobs that being processed in parallel.
        /// </summary>
        public int WorkersCount { get; set; }

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
        /// Starts the server and all it's workers.
        /// </summary>
        public virtual void Start()
        {
            if (_server != null)
            {
                throw new InvalidOperationException("Background job server has already been started. Please stop it first.");    
            }

            _server = new JobServer(MachineName, Queue, WorkersCount, PollInterval, JobActivator);
        }

        /// <summary>
        /// Stops the server and it's workers.
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
