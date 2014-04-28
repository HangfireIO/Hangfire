using System;
using System.Threading;
using HangFire.Server.Performing;

namespace HangFire.Server
{
    internal class Worker : IServerComponent
    {
        private readonly JobStorage _storage;
        private readonly WorkerContext _context;
        private readonly IJobPerformanceProcess _process;

        public Worker(JobStorage storage, WorkerContext context, IJobPerformanceProcess process)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (context == null) throw new ArgumentNullException("context");
            if (process == null) throw new ArgumentNullException("process");

            _storage = storage;
            _context = context;
            _process = process;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            using (var nextJob = connection.FetchNextJob(_context.QueueNames, cancellationToken))
            {
                nextJob.Process(_context, _process);

                // Checkpoint #4. The job was performed, and it is in the one
                // of the explicit states (Succeeded, Scheduled and so on).
                // It should not be re-queued, but we still need to remove its
                // processing information.
            }

            // Success point. No things must be done after previous command
            // was succeeded.
        }
    }
}