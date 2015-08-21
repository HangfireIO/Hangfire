using System;
using Hangfire.Common;
using Hangfire.States;
using Moq;

namespace Hangfire.Core.Tests
{
    public class StateContextMock
    {
        private readonly Lazy<StateContext> _context;

        public StateContextMock()
        {
            Storage = new Mock<JobStorage>();
            JobIdValue = "job-id";
            JobValue = Job.FromExpression(() => Console.WriteLine());
            CreatedAtValue = DateTime.UtcNow;

            _context = new Lazy<StateContext>(
                () => new StateContext(Storage.Object, JobIdValue, JobValue, CreatedAtValue));
        }

        public Mock<JobStorage> Storage { get; set; }
        public string JobIdValue { get; set; }
        public Job JobValue { get; set; }
        public DateTime CreatedAtValue { get; set; }

        public StateContext Object
        {
            get { return _context.Value; }
        }
    }
}
