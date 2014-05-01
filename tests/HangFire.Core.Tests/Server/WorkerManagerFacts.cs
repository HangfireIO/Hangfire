using System;
using System.Threading;
using HangFire.Server;
using HangFire.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class WorkerManagerFacts
    {
        private const int WorkerCount = 2;

        private readonly SharedWorkerContext _sharedContext;
        private readonly Mock<WorkerManager> _manager;
        private readonly Mock<IServerComponentRunner>[] _workerRunners;
        

        public WorkerManagerFacts()
        {
            _sharedContext = new SharedWorkerContext(
                "server",
                new[] { "default" },
                new Mock<JobStorage>().Object,
                new Mock<IJobPerformanceProcess>().Object,
                new Mock<JobActivator>().Object,
                new Mock<IStateMachineFactory>().Object);

            _workerRunners = new[]
            {
                new Mock<IServerComponentRunner>(), 
                new Mock<IServerComponentRunner>()
            };

            _manager = new Mock<WorkerManager>(
                _sharedContext, WorkerCount);

            _manager.Setup(x => x.CreateWorkerRunner(It.IsNotNull<WorkerContext>()))
                .Returns((WorkerContext context) => _workerRunners[context.WorkerNumber - 1].Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenSharedContextIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new WorkerManager(null, WorkerCount));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerCountLessOrEqualToZero()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new WorkerManager(
                    _sharedContext, 0));

            Assert.Equal("workerCount", exception.ParamName);
        }
        
        [Fact]
        public void CreateWorkerRunner_CreatesAWorkerRunnerWithGivenParameters()
        {
            var manager = new WorkerManager(_sharedContext, WorkerCount);
            var context = new WorkerContext(_sharedContext, 1);

            var worker = manager.CreateWorkerRunner(context);

            Assert.NotNull(worker);
        }

        [Fact]
        public void Execute_CallsStartMethodOnAllWorkers()
        {
            _manager.Object.Execute(new CancellationToken(true));

            _workerRunners[0].Verify(x => x.Start());
            _workerRunners[1].Verify(x => x.Start());
        }

        [Fact]
        public void Execute_CallsStopMethodOnAllWorkers()
        {
            _manager.Object.Execute(new CancellationToken(true));

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
