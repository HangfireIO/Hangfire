using System;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests
{
    class PerformContextMock
    {
        private readonly Lazy<PerformContext> _context;

        public PerformContextMock()
        {
            WorkerContext = new WorkerContextMock();
            Connection = new Mock<IStorageConnection>();
            JobId = "JobId";
            Job = Job.FromExpression(() => SomeMethod());
            CreatedAt = DateTime.UtcNow;
            CancellationToken = new Mock<IJobCancellationToken>();

            _context = new Lazy<PerformContext>(
                () => new PerformContext(WorkerContext.Object, Connection.Object, JobId, Job, CreatedAt, CancellationToken.Object));
        }

        public WorkerContextMock WorkerContext { get; set; }
        public Mock<IStorageConnection> Connection { get; set; }
        public string JobId { get; set; }
        public Job Job { get; set; }
        public DateTime CreatedAt { get; set; }
        public Mock<IJobCancellationToken> CancellationToken { get; set; } 

        public PerformContext Object
        {
            get { return _context.Value; }
        }

        public static void SomeMethod()
        {
        }
    }
}
