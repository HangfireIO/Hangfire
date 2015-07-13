using System;
using Hangfire.Server;

namespace Hangfire.Core.Tests
{
    public class WorkerContextMock
    {
        private readonly Lazy<WorkerContext> _context;

        public WorkerContextMock()
        {
            Queues = new[] { "default" };
            WorkerNumber = 1;

            _context = new Lazy<WorkerContext>(
                () => new WorkerContext(Queues, WorkerNumber));
        }

        public WorkerContext Object {get { return _context.Value; }}

        public string[] Queues { get; set; }
        public int WorkerNumber { get; set; }
    }
}
