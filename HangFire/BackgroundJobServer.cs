using System;
using HangFire.Server;

namespace HangFire
{
    public class BackgroundJobServer : IDisposable
    {
        private JobServer _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/>.
        /// </summary>
        public BackgroundJobServer()
        {
            ServerName = Environment.MachineName;
            WorkersCount = Environment.ProcessorCount * 2;
            QueueName = "default";
            PollInterval = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Gets or sets the server name. It should be unique across
        /// all server instances.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the queue to listen.
        /// </summary>
        public string QueueName { get; set; }

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
            if (_manager != null)
            {
                throw new InvalidOperationException("Background job server has already been started. Please stop it first.");    
            }

            _manager = new JobServer(ServerName, QueueName, WorkersCount, PollInterval, JobActivator);
        }

        /// <summary>
        /// Stops the server and it's workers.
        /// </summary>
        public virtual bool Stop()
        {
            if (_manager != null)
            {
                _manager.Dispose();
                _manager = null;

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
