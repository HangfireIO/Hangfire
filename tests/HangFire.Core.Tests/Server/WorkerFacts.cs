using System;
using System.Threading;
using HangFire.Server;
using HangFire.Server.Performing;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class WorkerFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly string[] _queues;
        private readonly WorkerContext _context;
        private readonly Mock<IJobPerformanceProcess> _process;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IProcessingJob> _processingJob;
        private readonly CancellationToken _token;

        public WorkerFacts()
        {
            _queues = new[] { "default" };
            _context = new WorkerContext("server", _queues, 1);
            _process = new Mock<IJobPerformanceProcess>();

            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _processingJob = new Mock<IProcessingJob>();

            _connection.Setup(x => x.FetchNextJob(_queues, It.IsNotNull<CancellationToken>()))
                .Returns(_processingJob.Object);

            _token = new CancellationToken();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker2(null, _context, _process.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker2(_storage.Object, null, _process.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker2(_storage.Object, _context, null));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact]
        public void Execute_TakesConnectionAndReleasesIt()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _storage.Verify(x => x.GetConnection(), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Execute_FetchesAJobAndReleasesIt()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _connection.Verify(
                x => x.FetchNextJob(_queues, _token),
                Times.Once);

            _processingJob.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Execute_StartsJobProcessing()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _processingJob.Verify(x => x.Process(_context, _process.Object));
        }

        private Worker2 CreateWorker()
        {
            return new Worker2(_storage.Object, _context, _process.Object);
        }
    }
}
