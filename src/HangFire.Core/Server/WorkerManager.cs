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
    internal class WorkerManager : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkerManager));

        private readonly SharedWorkerContext _sharedContext;
        private readonly int _workerCount;

        public WorkerManager(SharedWorkerContext sharedContext, int workerCount)
        {
            if (sharedContext == null) throw new ArgumentNullException("sharedContext");
            if (workerCount <= 0) throw new ArgumentOutOfRangeException("workerCount", "Worker count value must be more than zero.");

            _sharedContext = sharedContext;
            _workerCount = workerCount;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            var workerSupervisors = new List<IServerSupervisor>(_workerCount);
            for (var i = 1; i <= _workerCount; i++)
            {
                var workerContext = new WorkerContext(_sharedContext, i);

                // ReSharper disable once DoNotCallOverridableMethodsInConstructor
                workerSupervisors.Add(CreateWorkerSupervisor(workerContext));
            }

            using (var supervisors = new ServerSupervisorCollection(workerSupervisors))
            {
                supervisors.Start();
                cancellationToken.WaitHandle.WaitOne();
            }
        }

        public override string ToString()
        {
            return "Worker Manager";
        }

        internal virtual IServerSupervisor CreateWorkerSupervisor(WorkerContext context)
        {
            return new ServerSupervisor(
                new Worker(context),
                new ServerSupervisorOptions { LowerLogVerbosity = true });
        }
    }
}
