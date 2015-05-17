using System;
using Hangfire.Server;

namespace Hangfire.Core.Tests
{
    public class WorkerContextMock
    {
        private readonly Lazy<WorkerContext> _context;

        public WorkerContextMock()
        {
            ServerId = "server";
            Queues = new[] { "default" };
            WorkerNumber = 1;

            _context = new Lazy<WorkerContext>(
                () => new WorkerContext(ServerId, Queues, WorkerNumber));
        }

        public WorkerContext Object {get { return _context.Value; }}

        public string ServerId { get; set; }
        public string[] Queues { get; set; }
        public int WorkerNumber { get; set; }
    }
}
