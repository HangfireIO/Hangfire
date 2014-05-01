using System;
using HangFire.Server;

namespace HangFire.Core.Tests
{
    public class WorkerContextMock
    {
        private readonly Lazy<WorkerContext> _context;

        public WorkerContextMock()
        {
            SharedContext = new SharedWorkerContextMock();
            WorkerNumber = 1;

            _context = new Lazy<WorkerContext>(
                () => new WorkerContext(SharedContext.Object, WorkerNumber));
        }

        public WorkerContext Object {get { return _context.Value; }}

        public SharedWorkerContextMock SharedContext { get; set; }
        public int WorkerNumber { get; set; }
    }
}
