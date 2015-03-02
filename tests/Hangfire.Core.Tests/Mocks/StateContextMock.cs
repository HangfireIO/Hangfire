using System;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Core.Tests
{
    public class StateContextMock
    {
        private readonly Lazy<StateContext> _context;

        public StateContextMock()
        {
            JobIdValue = "job-id";
            JobValue = Job.FromExpression(() => Console.WriteLine());
            CreatedAtValue = DateTime.UtcNow;

            _context = new Lazy<StateContext>(
                () => new StateContext(JobIdValue, JobValue, CreatedAtValue));
        }

        public string JobIdValue { get; set; }
        public Job JobValue { get; set; }
        public DateTime CreatedAtValue { get; set; }

        public StateContext Object
        {
            get { return _context.Value; }
        }
    }
}
