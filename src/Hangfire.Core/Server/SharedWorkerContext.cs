// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Hangfire.States;

namespace Hangfire.Server
{
    public class SharedWorkerContext
    {
        internal SharedWorkerContext(
            string serverId,
            string[] queues,
            JobStorage storage,
            IJobPerformanceProcess performanceProcess,
            JobActivator activator,
            IStateMachineFactory stateMachineFactory)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (queues == null) throw new ArgumentNullException("queues");
            if (storage == null) throw new ArgumentNullException("storage");
            if (performanceProcess == null) throw new ArgumentNullException("performanceProcess");
            if (activator == null) throw new ArgumentNullException("activator");
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");

            ServerId = serverId;
            Queues = queues;
            Storage = storage;
            PerformanceProcess = performanceProcess;
            Activator = activator;
            StateMachineFactory = stateMachineFactory;
        }

        internal SharedWorkerContext(SharedWorkerContext context)
            : this(context.ServerId, context.Queues, context.Storage, context.PerformanceProcess, context.Activator, context.StateMachineFactory)
        {
        }

        public string ServerId { get; private set; }
        public string[] Queues { get; private set; }

        public JobStorage Storage { get; private set; }

        internal IJobPerformanceProcess PerformanceProcess { get; private set; }
        internal JobActivator Activator { get; private set; }
        internal IStateMachineFactory StateMachineFactory { get; private set; }
    }
}