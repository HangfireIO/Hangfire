using System;
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
    }
}