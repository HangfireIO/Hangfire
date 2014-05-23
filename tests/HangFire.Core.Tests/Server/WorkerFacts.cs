using System;
using System.Threading;
using HangFire.Common;
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
        private const string JobId = "my-job";
        private const string Queue = "my-queue";

        private readonly WorkerContextMock _context;
        private readonly Mock<IStorageConnection> _connection;
        private readonly CancellationToken _token;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IFetchedJob> _fetchedJob;
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IJobPerformanceProcess> _process;

        public WorkerFacts()
        {
            _context = new WorkerContextMock();
            _storage = _context.SharedContext.Storage;
            _process = _context.SharedContext.PerformanceProcess;

            _connection = new Mock<IStorageConnection>();

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _fetchedJob = new Mock<IFetchedJob>();
            _fetchedJob.Setup(x => x.JobId).Returns(JobId);

            _connection
                .Setup(x => x.FetchNextJob(_context.SharedContext.Queues, It.IsNotNull<CancellationToken>()))
                .Returns(_fetchedJob.Object);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    Job = Job.FromExpression(() => Method()),
                });

            _stateMachine = new Mock<IStateMachine>();

            _context.SharedContext.StateMachineFactory
                .Setup(x => x.Create(_connection.Object))
                .Returns(_stateMachine.Object);

            _stateMachine.Setup(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<IState>(),
                It.IsAny<string[]>())).Returns(true);

            _token = new CancellationToken();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(null));

            Assert.Equal("context", exception.ParamName);
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
        public void Execute_FetchesAJobAndRemovesItFromQueue()
        {
            var worker = CreateWorker();

            worker.Execute(_token);

            _connection.Verify(
                x => x.FetchNextJob(_context.SharedContext.Queues, _token),
                Times.Once);

            _fetchedJob.Verify(x => x.RemoveFromQueue());
        }

        [Fact]
        public void Execute_RequeuesAJob_WhenThereWasAnException()
        {
            _stateMachine
                .Setup(x => x.TryToChangeState(It.IsAny<string>(), It.IsAny<IState>(), It.IsAny<string[]>()))
                .Throws<InvalidOperationException>();

            var worker = CreateWorker();

            Assert.Throws<InvalidOperationException>(
                () => worker.Execute(_token));

            _fetchedJob.Verify(x => x.RemoveFromQueue(), Times.Never);
            _fetchedJob.Verify(x => x.Requeue());
        }

        [Fact, Sequence]
        public void Execute_ExecutesDefaultWorkflow_WhenJobIsCorrect()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.TryToChangeState(
                    JobId, It.IsAny<ProcessingState>(), It.IsAny<string[]>()))
                .InSequence()
                .Returns(true);

            _process.Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .InSequence();

            _stateMachine
                .Setup(x => x.TryToChangeState(
                    JobId, It.IsAny<SucceededState>(), It.IsAny<string[]>()))
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
                It.Is<ProcessingState>(state => state.ServerId == _context.Object.ServerId),
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
        public void Execute_DoesNotMoveAJob_ToTheFailedState_ButRequeuesIt_WhenProcessThrowsOperationCanceled()
        {
            // Arrange
            _process.Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .Throws<OperationCanceledException>();

            var worker = CreateWorker();

            // Act
            Assert.Throws<OperationCanceledException>(() => worker.Execute(_token));

            // Assert
            _stateMachine.Verify(
                x => x.TryToChangeState(It.IsAny<string>(), It.IsAny<FailedState>(), It.IsAny<string[]>()),
                Times.Never);
            _fetchedJob.Verify(x => x.Requeue());
        }

        [Fact]
        public void Execute_RemovesJobFromQueue_WhenProcessThrowsJobAbortedException()
        {
            // Arrange
            _process.Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .Throws<JobAbortedException>();

            var worker = CreateWorker();

            // Act
            Assert.DoesNotThrow(() => worker.Execute(_token));

            _fetchedJob.Verify(x => x.RemoveFromQueue());
            _fetchedJob.Verify(x => x.Requeue(), Times.Never);
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
                JobId,
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
                JobId,
                It.Is<FailedState>(state => state.Exception == exception && state.Reason == "hello"),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasJobLoadException()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { LoadException = new JobLoadException("asd", new Exception()) });

            var worker = CreateWorker();

            // Act
            worker.Execute(_token);

            // Assert
            _stateMachine.Verify(x => x.TryToChangeState(
                JobId,
                It.IsAny<FailedState>(),
                It.IsAny<string[]>()));
        }

        private Worker CreateWorker()
        {
            return new Worker(_context.Object);
        }

        public static void Method() { }
    }
}
