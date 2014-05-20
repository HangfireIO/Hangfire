using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;

namespace HangFire.Core.Tests
{
    public class StateContextMock
    {
        private readonly Lazy<StateContext> _context;

        public StateContextMock()
        {
            JobIdValue = "job-id";
            JobValue = Job.FromExpression(() => Console.WriteLine());
            CreatedAtValue = DateTime.UtcNow;
            ConnectionValue = new Mock<IStorageConnection>();

            _context = new Lazy<StateContext>(
                () => new StateContext(JobIdValue, JobValue, CreatedAtValue, ConnectionValue.Object));
        }

        public string JobIdValue { get; set; }
        public Job JobValue { get; set; }
        public DateTime CreatedAtValue { get; set; }

        public Mock<IStorageConnection> ConnectionValue { get; set; } 

        public StateContext Object
        {
            get { return _context.Value; }
        }
    }
}
