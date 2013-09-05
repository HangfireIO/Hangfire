using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    internal class JobInvoker
    {
        private readonly HangFireJobActivator _activator;
        private readonly IEnumerable<IServerFilter> _filters;

        public JobInvoker(HangFireJobActivator activator, IEnumerable<IServerFilter> filters)
        {
            _activator = activator;
            _filters = filters;
        }

        public void ProcessJob(JobDescription jobDescription)
        {
            using (var worker = _activator.ActivateJob(jobDescription.WorkerType))
            {
                worker.Args = jobDescription.Args;

                // ReSharper disable once AccessToDisposedClosure
                InvokeFilters(worker, worker.Perform);
            }
        }

        private void InvokeFilters(HangFireJob job, Action action)
        {
            var commandAction = action;

            var entries = _filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var innerAction = commandAction;
                var currentEntry = entry;

                commandAction = () => currentEntry.InterceptPerform(job, innerAction);
            }

            commandAction();
        }
    }
}
