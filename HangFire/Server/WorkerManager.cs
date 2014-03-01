// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class WorkerManager : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkerManager));

        private readonly DisposableCollection<Worker> _workers;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        public WorkerManager(ServerContext context, int workerCount)
        {
            _workers = new DisposableCollection<Worker>();

            for (var i = 1; i <= workerCount; i++)
            {
                var workerContext = new WorkerContext(context, i);

                var worker = new Worker(workerContext);
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
