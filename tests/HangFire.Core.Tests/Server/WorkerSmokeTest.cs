using System;
using System.Threading;
using HangFire.Server;
using HangFire.Server.Performing;
using HangFire.Storage;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace HangFire.Core.Tests.Server
{
    public class WorkerSmokeTest
    {
        private const string Server = "server";
        private static readonly string[] Queues = { "critical", "default" };
        private const int WorkerNumber = 1;

        private readonly WorkerContext _context;
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IJobPerformanceProcess> _process;
        private readonly Mock<ProcessingJob> _processingJob;

        public WorkerSmokeTest()
        {
            _context = new WorkerContext(Server, Queues, WorkerNumber);
            _storage = new Mock<JobStorage> { CallBase = false };

            var connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(connection.Object);
            _process = new Mock<IJobPerformanceProcess>();

            _processingJob = new Mock<ProcessingJob>(connection.Object, "1", "default")
            {
                CallBase = false
            };

            connection
                .Setup(x => x.FetchNextJob(Queues, It.IsAny<CancellationToken>()))
                .Returns(() => { Thread.Sleep(10); return _processingJob.Object; });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(null, _context, _process.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_storage.Object, null, _process.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_storage.Object, _context, null));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact(Timeout = 20 * 1000)]
        public void Start_InitiatesJobFetchingAndProcessing()
        {
            using (var worker = new Worker(_storage.Object, _context, _process.Object))
            {
                worker.Start();
                Thread.Sleep(100);
            }

            _processingJob.Verify(
                x => x.Process(_context, _process.Object),
                Times.AtLeast(2));
        }
    }
}
