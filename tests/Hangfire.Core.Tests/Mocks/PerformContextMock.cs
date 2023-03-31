using System;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests
{
    public class PerformContextMock
    {
        private readonly Lazy<PerformContext> _context;

        public PerformContextMock()
        {
            Storage = new Mock<JobStorage>();
            Connection = new Mock<IStorageConnection>();
            BackgroundJob = new BackgroundJobMock();
            CancellationToken = new Mock<IJobCancellationToken>();

            _context = new Lazy<PerformContext>(
                () => new PerformContext(Storage.Object, Connection.Object, BackgroundJob.Object, CancellationToken.Object));
        }
        
        public Mock<JobStorage> Storage { get; set; }
        public Mock<IStorageConnection> Connection { get; set; }
        public BackgroundJobMock BackgroundJob { get; set; }
        public Mock<IJobCancellationToken> CancellationToken { get; set; } 

        public PerformContext Object => _context.Value;

        public static void SomeMethod()
        {
        }

        public PerformingContext GetPerformingContext()
        {
            return new PerformingContext(Object);
        }

        public PerformedContext GetPerformedContext(object result = null, bool canceled = false, Exception exception = null)
        {
            return new PerformedContext(Object, result, canceled, exception);
        }
    }
}
