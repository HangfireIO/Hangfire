using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HangFire.Server;
using HangFire.States;

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
            Queues = queues ?? new[] { EnqueuedState.DefaultQueue };
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

            var serverName = String.Format("{0}:{1}", MachineName.ToLowerInvariant(), Process.GetCurrentProcess().Id);

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
