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
        private readonly Mock<IServerSupervisor>[] _workerSupervisors;
        

        public WorkerManagerFacts()
        {
            _sharedContext = new SharedWorkerContext(
                "server",
                new[] { "default" },
                new Mock<JobStorage>().Object,
                new Mock<IJobPerformanceProcess>().Object,
                new Mock<JobActivator>().Object,
                new Mock<IStateMachineFactory>().Object);

            _workerSupervisors = new[]
            {
                new Mock<IServerSupervisor>(), 
                new Mock<IServerSupervisor>()
            };

            _manager = new Mock<WorkerManager>(
                _sharedContext, WorkerCount);

            _manager.Setup(x => x.CreateWorkerSupervisor(It.IsNotNull<WorkerContext>()))
                .Returns((WorkerContext context) => _workerSupervisors[context.WorkerNumber - 1].Object);
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
        public void CreateWorkerSupervisor_CreatesAWorkerSupervisorWithGivenParameters()
        {
            var manager = new WorkerManager(_sharedContext, WorkerCount);
            var context = new WorkerContext(_sharedContext, 1);

            var worker = manager.CreateWorkerSupervisor(context);

            Assert.NotNull(worker);
        }

        [Fact]
        public void Execute_CallsStartMethodOnAllWorkers()
        {
            _manager.Object.Execute(new CancellationToken(true));

            _workerSupervisors[0].Verify(x => x.Start());
            _workerSupervisors[1].Verify(x => x.Start());
        }

        [Fact]
        public void Execute_CallsStopMethodOnAllWorkers()
        {
            _manager.Object.Execute(new CancellationToken(true));

            _workerSupervisors[0].Verify(x => x.Stop());
            _workerSupervisors[1].Verify(x => x.Stop());
        }

        [Fact]
        public void Execute_CallsDisposeMethodOnAllWorkers()
        {
            _manager.Object.Execute(new CancellationToken(true));

            _workerSupervisors[0].Verify(x => x.Dispose());
            _workerSupervisors[1].Verify(x => x.Dispose());
        }
    }
}
