using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    class JobProcessor
    {
        private readonly WorkerActivator _activator;
        private readonly IEnumerable<IServerFilter> _interceptors;

        public JobProcessor(WorkerActivator activator, IEnumerable<IServerFilter> interceptors)
        {
            _activator = activator;
            _interceptors = interceptors;
        }

        public void ProcessJob(Job job)
        {
            using (var worker = _activator.CreateWorker(job.WorkerType))
            {
                worker.Args = job.Args;

                // ReSharper disable once AccessToDisposedClosure
                InvokeInterceptors(worker, worker.Perform);
            }
        }

        private void InvokeInterceptors(Worker worker, Action action)
        {
            var commandAction = action;

            var entries = _interceptors.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var innerAction = commandAction;
                var currentEntry = entry;

                commandAction = () => currentEntry.InterceptPerform(worker, innerAction);
            }

            commandAction();
        }
    }
}
