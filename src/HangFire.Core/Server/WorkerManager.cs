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
using System.Threading;
using Common.Logging;

namespace HangFire.Server
{
    internal class WorkerManager : IServerComponent, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkerManager));

        private readonly ServerComponentRunnerCollection _workerRunners;

        public WorkerManager(SharedWorkerContext sharedContext, int workerCount)
        {
            if (sharedContext == null) throw new ArgumentNullException("sharedContext");

            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");

            var workerRunners = new List<IServerComponentRunner>(workerCount);
            for (var i = 1; i <= workerCount; i++)
            {
                var workerContext = new WorkerContext(sharedContext, i);

// ReSharper disable once DoNotCallOverridableMethodsInConstructor
                workerRunners.Add(CreateWorkerRunner(workerContext));
            }

            _workerRunners = new ServerComponentRunnerCollection(workerRunners);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            try
            {
                _workerRunners.Start();

                cancellationToken.WaitHandle.WaitOne();
            }
            finally
            {
                _workerRunners.Stop();
            }
        }

        public void Dispose()
        {
            _workerRunners.Dispose();
        }

        public override string ToString()
        {
            return "Worker Manager";
        }

        internal virtual IServerComponentRunner CreateWorkerRunner(WorkerContext context)
        {
            return new ServerComponentRunner(
                new Worker(context),
                new ServerComponentRunnerOptions { LowerLogVerbosity = true });
        }
    }
}
