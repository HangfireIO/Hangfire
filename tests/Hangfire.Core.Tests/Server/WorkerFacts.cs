using System;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class WorkerFacts
    {
        private const string JobId = "my-job";

        private readonly WorkerContextMock _workerContext;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IFetchedJob> _fetchedJob;
        private readonly Mock<IJobPerformanceProcess> _process;
        private readonly Mock<IStateMachineFactoryFactory> _stateMachineFactoryFactory; 
        private readonly BackgroundProcessContextMock _context;

        public WorkerFacts()
        {
            _context = new BackgroundProcessContextMock();
            _workerContext = new WorkerContextMock();
            _process = new Mock<IJobPerformanceProcess>();
            var stateMachineFactory = new Mock<IStateMachineFactory>();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _fetchedJob = new Mock<IFetchedJob>();
            _fetchedJob.Setup(x => x.JobId).Returns(JobId);

            _connection
                .Setup(x => x.FetchNextJob(_workerContext.Queues, It.IsNotNull<CancellationToken>()))
                .Returns(_fetchedJob.Object);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    Job = Job.FromExpression(() => Method()),
                });

            _stateMachine = new Mock<IStateMachine>();

            stateMachineFactory
                .Setup(x => x.Create(_connection.Object))
                .Returns(_stateMachine.Object);

            _stateMachine.Setup(x => x.ChangeState(
                It.IsAny<string>(),
                It.IsAny<IState>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>())).Returns(true);

            _stateMachineFactoryFactory = new Mock<IStateMachineFactoryFactory>();
            _stateMachineFactoryFactory
                .Setup(x => x.CreateFactory(It.IsNotNull<JobStorage>()))
                .Returns(stateMachineFactory.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(null, _process.Object, _stateMachineFactoryFactory.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_workerContext.Object, null, _stateMachineFactoryFactory.Object));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactory_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_workerContext.Object, _process.Object, null));

            Assert.Equal("stateMachineFactoryFactory", exception.ParamName);
        }

        [Fact]
        public void Execute_TakesConnectionAndReleasesIt()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _context.Storage.Verify(x => x.GetConnection(), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Execute_FetchesAJobAndRemovesItFromQueue()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _connection.Verify(
                x => x.FetchNextJob(_workerContext.Queues, _context.CancellationTokenSource.Token),
                Times.Once);

            _fetchedJob.Verify(x => x.RemoveFromQueue());
        }

        [Fact]
        public void Execute_RequeuesAJob_WhenThereWasAnException()
        {
            _stateMachine
                .Setup(x => x.ChangeState(It.IsAny<string>(), It.IsAny<IState>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Throws<InvalidOperationException>();

            var worker = CreateWorker();

            Assert.Throws<InvalidOperationException>(
                () => worker.Execute(_context.Object));

            _fetchedJob.Verify(x => x.RemoveFromQueue(), Times.Never);
            _fetchedJob.Verify(x => x.Requeue());
        }

        [Fact, Sequence]
        public void Execute_ExecutesDefaultWorkflow_WhenJobIsCorrect()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.ChangeState(
                    JobId, It.IsAny<ProcessingState>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .InSequence()
                .Returns(true);

            _process.Setup(x => x.Run(It.IsAny<PerformContext>()))
                .InSequence();

            _stateMachine
                .Setup(x => x.ChangeState(
                    JobId, It.IsAny<SucceededState>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .InSequence()
                .Returns(true);

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Execute_SetsCurrentServer_ToProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateMachine.Verify(x => x.ChangeState(
                It.IsAny<string>(),
                It.Is<ProcessingState>(state => state.ServerId == _context.ServerId),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void Execute_ProcessesOnlyJobs_InEnqueuedAndProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateMachine.Verify(x => x.ChangeState(
                It.IsAny<string>(),
                It.IsAny<ProcessingState>(),
                It.Is<string[]>(
                    states => states.Length == 2 &&
                        states[0] == EnqueuedState.StateName && states[1] == ProcessingState.StateName),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void Execute_DoesNotRun_PerformanceProcess_IfTransitionToProcessingStateFailed()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.ChangeState(
                    It.IsAny<string>(),
                    It.IsAny<ProcessingState>(),
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(false);

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _process.Verify(x => x.Run(It.IsAny<PerformContext>()), Times.Never);
        }

        [Fact]
        public void Execute_Runs_PerformanceProcess()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _process.Verify(x => x.Run(It.IsNotNull<PerformContext>()));
        }

        [Fact]
        public void Execute_DoesNotMoveAJob_ToTheFailedState_ButRequeuesIt_WhenProcessThrowsOperationCanceled()
        {
            // Arrange
            _process.Setup(x => x.Run(It.IsAny<PerformContext>()))
                .Throws<OperationCanceledException>();

            var worker = CreateWorker();

            // Act
            Assert.Throws<OperationCanceledException>(() => worker.Execute(_context.Object));

            // Assert
            _stateMachine.Verify(
                x => x.ChangeState(It.IsAny<string>(), It.IsAny<FailedState>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _fetchedJob.Verify(x => x.Requeue());
        }

        [Fact]
        public void Execute_RemovesJobFromQueue_WhenProcessThrowsJobAbortedException()
        {
            // Arrange
            _process.Setup(x => x.Run(It.IsAny<PerformContext>()))
                .Throws<JobAbortedException>();

            var worker = CreateWorker();

            // Act
            Assert.DoesNotThrow(() => worker.Execute(_context.Object));

            _fetchedJob.Verify(x => x.RemoveFromQueue());
            _fetchedJob.Verify(x => x.Requeue(), Times.Never);
        }

        [Fact]
        public void Execute_MovesJob_ToSuccessfulState_OnlyIfItIsInProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateMachine.Verify(x => x.ChangeState(
                It.IsAny<string>(),
                It.IsAny<SucceededState>(),
                It.Is<string[]>(states => states.Length == 1 && states[0] == ProcessingState.StateName),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasInternalException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _process
                .Setup(x => x.Run(It.IsAny<PerformContext>()))
                .Throws(exception);

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ChangeState(
                JobId,
                It.Is<FailedState>(state => state.Exception == exception && state.Reason.Contains("Internal")),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasUserException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _process
                .Setup(x => x.Run(It.IsAny<PerformContext>()))
                .Throws(new JobPerformanceException("hello", exception));

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ChangeState(
                JobId,
                It.Is<FailedState>(state => state.Exception == exception && state.Reason == "hello"),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasJobLoadException()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { LoadException = new JobLoadException("asd", new Exception()) });

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ChangeState(
                JobId,
                It.IsAny<FailedState>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()));
        }

        private Worker CreateWorker()
        {
            return new Worker(_workerContext.Object, _process.Object, _stateMachineFactoryFactory.Object);
        }

        public static void Method() { }
    }
}
