using System;
using System.Collections.Generic;

namespace HangFire.Server
{
    public class ServerContext
    {
        internal ServerContext(ServerContext context)
            : this(context.ServerName, context.Queues, context.WorkersCount, context.PollInterval, context.Activator, context.Performer)
        {
        }

        internal ServerContext(
            string serverName, 
            IList<string> queues, 
            int workersCount,
            TimeSpan pollInterval,
            JobActivator activator,
            JobPerformer performer)
        {
            ServerName = serverName;
            Queues = queues;
            WorkersCount = workersCount;
            PollInterval = pollInterval;

            Activator = activator;
            Performer = performer;
        }

        public string ServerName { get; private set; }
        public IList<string> Queues { get; private set; }
        public int WorkersCount { get; private set; }
        public TimeSpan PollInterval { get; private set; }

        internal JobActivator Activator { get; private set; }
        internal JobPerformer Performer { get; private set; }
    }
}
