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
using System.Collections.Generic;
using HangFire.Server.Performing;

namespace HangFire.Server
{
    internal class WorkerManager : IServerComponentRunner
    {
        private readonly JobStorage _storage;
        private readonly IJobPerformanceProcess _performanceProcess;
        private readonly ServerComponentRunnerCollection _workerRunners;

        public WorkerManager(
            string serverId,
            int workerCount, 
            string[] queues,
            JobStorage storage, 
            IJobPerformanceProcess performanceProcess)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (storage == null) throw new ArgumentNullException("storage");
            if (performanceProcess == null) throw new ArgumentNullException("performanceProcess");
            if (queues == null) throw new ArgumentNullException("queues");
            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");

            _storage = storage;
            _performanceProcess = performanceProcess;

            var workerRunners = new List<IServerComponentRunner>(workerCount);
            for (var i = 1; i <= workerCount; i++)
            {
                var workerContext = new WorkerContext(serverId, queues, i);

// ReSharper disable once DoNotCallOverridableMethodsInConstructor
                workerRunners.Add(CreateWorkerRunner(workerContext));
            }

            _workerRunners = new ServerComponentRunnerCollection(workerRunners);
        }

        public void Start()
        {
            _workerRunners.Start();
        }

        public void Stop()
        {
            _workerRunners.Stop();
        }

        public void Dispose()
        {
            _workerRunners.Dispose();
        }

        internal virtual IServerComponentRunner CreateWorkerRunner(WorkerContext context)
        {
            return new ServerComponentRunner(
                new Worker(_storage, context, _performanceProcess),
                new ServerComponentRunnerOptions { MinimumLogVerbosity = true });
        }
    }
}
