using System;
using System.Linq;
using Common.Logging;
using HangFire.States;

namespace HangFire
{
    public class BackgroundJobServerOptions
    {
        private string _serverName;
        private int _workerCount;
        private string[] _queues;

        public BackgroundJobServerOptions()
        {
            WorkerCount = Environment.ProcessorCount * 5;
            ServerName = Environment.MachineName;
            Queues = new[] { EnqueuedState.DefaultQueue };
            ShutdownTimeout = TimeSpan.FromSeconds(15);
            SchedulePollingInterval = TimeSpan.FromSeconds(15);
        }

        public string ServerName
        {
            get { return _serverName; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");

                _serverName = value;
            }
        }

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

        public void Log(ILog logger)
        {
            logger.InfoFormat("Using the following options for HangFire Server:");
            logger.InfoFormat("    Worker count: {0}.", WorkerCount);
            logger.InfoFormat("    Listening queues: {0}.", String.Join(", ", Queues.Select(x => "'" + x + "'")));
            logger.InfoFormat("    Shutdown timeout: {0}.", ShutdownTimeout);
            logger.InfoFormat("    Schedule polling interval: {0}.", SchedulePollingInterval);
        }
    }
}