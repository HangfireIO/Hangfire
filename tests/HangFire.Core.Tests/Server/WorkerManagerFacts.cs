using System;
using HangFire.Server;
using HangFire.Server.Performing;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class WorkerManagerFacts
    {
        private const string ServerId = "server";
        private const int WorkerCount = 2;
        private static readonly string[] Queues = { "default" };

        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IJobPerformanceProcess> _process;
        private readonly Mock<WorkerManager> _manager;
        private readonly Mock<IServerComponentRunner>[] _workerRunners;

        public WorkerManagerFacts()
        {
            _storage = new Mock<JobStorage>();
            _process = new Mock<IJobPerformanceProcess>();

            _workerRunners = new[]
            {
                new Mock<IServerComponentRunner>(), 
                new Mock<IServerComponentRunner>()
            };

            _manager = new Mock<WorkerManager>(
                ServerId, WorkerCount, Queues, _storage.Object, _process.Object);

            _manager.Setup(x => x.CreateWorkerRunner(It.IsNotNull<WorkerContext>()))
                .Returns((WorkerContext context) => _workerRunners[context.WorkerNumber - 1].Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(null, WorkerCount, Queues, _storage.Object, _process.Object));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerCountLessOrEqualToZero()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new WorkerManager(ServerId, 0, Queues, _storage.Object, _process.Object));

            Assert.Equal("workerCount", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesArrayIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(ServerId, WorkerCount, null, _storage.Object, _process.Object));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(ServerId, WorkerCount, Queues, null, _process.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(ServerId, WorkerCount, Queues, _storage.Object, null));

            Assert.Equal("performanceProcess", exception.ParamName);
        }

        [Fact]
        public void CreateWorkerRunner_CreatesAWorkerRunnerWithGivenParameters()
        {
            var manager = new WorkerManager(ServerId, WorkerCount, Queues, _storage.Object, _process.Object);
            var context = new WorkerContext(ServerId, Queues, 1);

            var worker = manager.CreateWorkerRunner(context);

            Assert.NotNull(worker);
        }

        [Fact]
        public void Start_CallsStartMethodOnAllWorkers()
        {
            _manager.Object.Start();

            _workerRunners[0].Verify(x => x.Start());
            _workerRunners[1].Verify(x => x.Start());
        }

        [Fact]
        public void Stop_CallsStopMethodOnAllWorkers()
        {
            _manager.Object.Stop();

            _workerRunners[0].Verify(x => x.Stop());
            _workerRunners[1].Verify(x => x.Stop());
        }

        [Fact]
        public void Dispose_CallsDisposeMethodOnAllWorkers()
        {
            _manager.Object.Dispose();

            _workerRunners[0].Verify(x => x.Dispose());
            _workerRunners[1].Verify(x => x.Dispose());
        }
    }
}
