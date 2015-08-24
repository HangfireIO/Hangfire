using System;
using System.Linq;
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

            _stateMachine.Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Returns<StateChangeContext>(ctx => ctx.NewState);

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
                .Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
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
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == JobId && ctx.NewState is ProcessingState)))
                .InSequence()
                .Returns<StateChangeContext>(ctx => ctx.NewState);

            _process.Setup(x => x.Run(It.IsAny<PerformContext>()))
                .InSequence();

            _stateMachine
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == JobId && ctx.NewState is SucceededState)))
                .InSequence()
                .Returns<StateChangeContext>(context => context.NewState);

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

            _stateMachine.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is ProcessingState && (((ProcessingState) ctx.NewState).ServerId == _context.ServerId))));
        }

        [Fact]
        public void Execute_ProcessesOnlyJobs_InEnqueuedAndProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateMachine.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is ProcessingState &&
                ctx.ExpectedStates.ElementAt(0) == EnqueuedState.StateName &&
                ctx.ExpectedStates.ElementAt(1) == ProcessingState.StateName)));
        }

        [Fact]
        public void Execute_DoesNotRun_PerformanceProcess_IfTransitionToProcessingStateFailed()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is ProcessingState)))
                .Returns<IState>(null);

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
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is FailedState)),
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

            _stateMachine.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is SucceededState &&
                ctx.ExpectedStates.ElementAt(0) == ProcessingState.StateName)));
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
            _stateMachine.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is FailedState &&
                ((FailedState) ctx.NewState).Exception == exception)));
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
            _stateMachine.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is FailedState)));
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
            _stateMachine.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is FailedState)));
        }

        private Worker CreateWorker()
        {
            return new Worker(_workerContext.Object, _process.Object, _stateMachineFactoryFactory.Object);
        }

        public static void Method() { }
    }
}
