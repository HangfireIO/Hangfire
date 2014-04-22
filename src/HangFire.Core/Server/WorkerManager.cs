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
using System.Threading;
using Common.Logging;

namespace HangFire.Server
{
    internal class WorkerManager : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkerManager));

        private readonly DisposableCollection<Worker> _workers;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        public WorkerManager(
            JobStorage storage, 
            string serverName,
            string[] queueNames,
            int workerCount)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverName == null) throw new ArgumentNullException("serverName");
            if (queueNames == null) throw new ArgumentNullException("queueNames");

            if (workerCount <= 0) throw new ArgumentException("Worker count value must be greater than zero", "workerCount");

            _workers = new DisposableCollection<Worker>();

            for (var i = 1; i <= workerCount; i++)
            {
                var workerContext = new WorkerContext(serverName, queueNames, i);

                var worker = new Worker(storage, workerContext);
                worker.Start();

                _workers.Add(worker);
            }
        }

        public void Dispose()
        {
            Logger.Info("Stopping workers...");

            _workers.Dispose();

            Logger.Info("Workers were stopped.");

            _cts.Dispose();
        }
    }
}
