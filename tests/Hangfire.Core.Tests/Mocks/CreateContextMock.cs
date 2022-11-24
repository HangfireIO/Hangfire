using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests
{
    public class CreateContextMock
    {
        private readonly Lazy<CreateContext> _context;

        public CreateContextMock()
        {
            Storage = new Mock<JobStorage>();
            Connection = new Mock<IStorageConnection>();
            Job = Job.FromExpression(() => Method());
            InitialState = new Mock<IState>();

            _context = new Lazy<CreateContext>(
                () => new CreateContext(
                    Storage.Object,
                    Connection.Object,
                    Job,
                    InitialState.Object));
        }

        public Mock<JobStorage> Storage { get; set; }
        public Mock<IStorageConnection> Connection { get; set; }
        public Job Job { get; set; }
        public Mock<IState> InitialState { get; set; } 

        public CreateContext Object => _context.Value;

        public static void Method() { }

        public CreatingContext GetCreatingContext()
        {
            return new CreatingContext(Object);
        }

        public CreatedContext GetCreatedContext(
            string jobId, DateTime? createdAt = null, bool canceled = false, Exception exception = null)
        {
            return new CreatedContext(
                Object,
                new BackgroundJob(jobId, Job, createdAt ?? DateTime.UtcNow),
                canceled,
                exception);
        }
    }
}
