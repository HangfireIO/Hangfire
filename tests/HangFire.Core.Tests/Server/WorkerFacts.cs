using System;
using System.Threading;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Server;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Moq.Sequences;
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
        private readonly CancellationToken _token;
        private readonly Mock<IStateMachineFactory> _stateMachineFactory;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly ProcessingJob _processingJob;

        public WorkerFacts()
        {
            _queues = new[] { "default" };
            _context = new WorkerContext("server", _queues, 1);
            _process = new Mock<IJobPerformanceProcess>();

            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _processingJob = new ProcessingJob("my-job", "my-queue");

            _connection.Setup(x => x.FetchNextJob(_queues, It.IsNotNull<CancellationToken>()))
                .Returns(_processingJob);

            _stateMachineFactory = new Mock<IStateMachineFactory>();
            _stateMachine = new Mock<IStateMachine>();

            _stateMachineFactory.Setup(x => x.Create(_connection.Object))
                .Returns(_stateMachine.Object);

            _stateMachine.Setup(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<State>(),
                It.IsAny<string[]>())).Returns(true);

            _connection.Setup(x => x.GetJobData(_processingJob.JobId))
                .Returns(new JobData
                {
                    Job = Job.FromExpression(() => Method()),
                });


            _token = new CancellationToken();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(null, _storage.Object, _process.Object, _stateMachineFactory.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_context, null, _process.Object, _stateMachineFactory.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_context, _storage.Object, null, _stateMachineFactory.Object));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_context, _storage.Object, _process.Object, null));

            Assert.Equal("stateMachineFactory", exception.ParamName);
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
        public void Execute_FetchesAJobAndDeletesItFromQueue()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _connection.Verify(
                x => x.FetchNextJob(_queues, _token),
                Times.Once);

            _connection.Verify(x => x.DeleteJobFromQueue(_processingJob.JobId, _processingJob.Queue));
        }

        [Fact, Sequence]
        public void Execute_ExecutesDefaultWorkflow_WhenJobIsCorrect()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.TryToChangeState(
                    _processingJob.JobId, It.IsAny<ProcessingState>(), It.IsAny<string[]>()))
                .InSequence()
                .Returns(true);

            _process.Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .InSequence();

            _stateMachine
                .Setup(x => x.TryToChangeState(
                    _processingJob.JobId, It.IsAny<SucceededState>(), It.IsAny<string[]>()))
                .InSequence()
                .Returns(true);

            var worker = CreateWorker();

            // Act
            worker.Execute(_token);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Execute_SetsCurrentServer_ToProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _stateMachine.Verify(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.Is<ProcessingState>(state => state.ServerName == _context.ServerName),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Execute_ProcessesOnlyJobs_InEnqueuedAndProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _stateMachine.Verify(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<ProcessingState>(),
                It.Is<string[]>(
                    states => states.Length == 2 &&
                        states[0] == EnqueuedState.StateName && states[1] == ProcessingState.StateName)));
        }

        [Fact]
        public void Execute_DoesNotRun_PerformanceProcess_IfTransitionToProcessingStateFailed()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.TryToChangeState(
                    It.IsAny<string>(),
                    It.IsAny<ProcessingState>(),
                    It.IsAny<string[]>()))
                .Returns(false);

            var worker = CreateWorker();

            // Act
            worker.Execute(_token);

            // Assert
            _process.Verify(
                x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()),
                Times.Never);
        }

        [Fact]
        public void Execute_Runs_PerformanceProcess()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _process.Verify(x => x.Run(
                It.IsNotNull<PerformContext>(),
                It.IsNotNull<IJobPerformer>()));
        }

        [Fact]
        public void Execute_MovesJob_ToSuccessfulState_OnlyIfItIsInProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _stateMachine.Verify(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<SucceededState>(),
                It.Is<string[]>(states => states.Length == 1 && states[0] == ProcessingState.StateName)));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasInternalException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _process
                .Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .Throws(exception);

            var worker = CreateWorker();

            // Act
            worker.Execute(_token);

            // Assert
            _stateMachine.Verify(x => x.TryToChangeState(
                _processingJob.JobId,
                It.Is<FailedState>(state => state.Exception == exception && state.Reason.Contains("Internal")),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasUserException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _process
                .Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .Throws(new JobPerformanceException("hello", exception));

            var worker = CreateWorker();

            // Act
            worker.Execute(_token);

            // Assert
            _stateMachine.Verify(x => x.TryToChangeState(
                _processingJob.JobId,
                It.Is<FailedState>(state => state.Exception == exception && state.Reason == "hello"),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasJobLoadException()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(_processingJob.JobId))
                .Returns(new JobData { LoadException = new JobLoadException() });

            var worker = CreateWorker();

            // Act
            worker.Execute(_token);

            // Assert
            _stateMachine.Verify(x => x.TryToChangeState(
                _processingJob.JobId,
                It.IsAny<FailedState>(),
                It.IsAny<string[]>()));
        }

        private Worker CreateWorker()
        {
            return new Worker(_context, _storage.Object, _process.Object, _stateMachineFactory.Object);
        }

        public static void Method() { }
    }
}
