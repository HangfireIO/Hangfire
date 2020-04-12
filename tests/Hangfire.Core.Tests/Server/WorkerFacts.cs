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

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class WorkerFacts
    {
        private const string JobId = "my-job";

        private readonly string[] _queues;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IBackgroundJobStateChanger> _stateChanger;
        private readonly Mock<IFetchedJob> _fetchedJob;
        private readonly Mock<IBackgroundJobPerformer> _performer;
        private readonly BackgroundProcessContextMock _context;

        public WorkerFacts()
        {
            _context = new BackgroundProcessContextMock();
            _queues = new[] {"critical"};
            _performer = new Mock<IBackgroundJobPerformer>();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _fetchedJob = new Mock<IFetchedJob>();
            _fetchedJob.Setup(x => x.JobId).Returns(JobId);

            _connection
                .Setup(x => x.FetchNextJob(_queues, It.IsNotNull<CancellationToken>()))
                .Returns(_fetchedJob.Object);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    Job = Job.FromExpression(() => Method()),
                });

            _stateChanger = new Mock<IBackgroundJobStateChanger>();
            _stateChanger.Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Returns<StateChangeContext>(ctx => ctx.NewState);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesCollectionNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(null, _performer.Object, _stateChanger.Object));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPerformanceProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_queues, null, _stateChanger.Object));

            Assert.Equal("performer", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangeProcess_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new Worker(_queues, _performer.Object, null));

            Assert.Equal("stateChanger", exception.ParamName);
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
                x => x.FetchNextJob(_queues, _context.StoppingTokenSource.Token),
                Times.Once);

            _fetchedJob.Verify(x => x.RemoveFromQueue());
        }

        [Fact]
        public void Execute_RequeuesAJob_WhenThereWasAnException()
        {
            _stateChanger
                .Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Throws<InvalidOperationException>();

            var worker = CreateWorker(1);

            Assert.Throws<InvalidOperationException>(
                () => worker.Execute(_context.Object));

            _fetchedJob.Verify(x => x.RemoveFromQueue(), Times.Never);
            _fetchedJob.Verify(x => x.Requeue());
        }

        [Fact]
        public void Execute_MovesAJobToTheFailedState_WithFiltersDisabled_WhenStateChangerThrowsAnException()
        {
            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(y => y.NewState.Name != FailedState.StateName)))
                .Throws<InvalidOperationException>();

            var worker = CreateWorker(1);

            worker.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(y =>
                y.NewState.Name == FailedState.StateName &&
                y.DisableFilters == true)));

            _fetchedJob.Verify(x => x.RemoveFromQueue(), Times.Once);
            _fetchedJob.Verify(x => x.Requeue(), Times.Never);
        }

        [Fact, Sequence]
        public void Execute_ExecutesDefaultWorkflow_WhenJobIsCorrect()
        {
            // Arrange
            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == JobId && ctx.NewState is ProcessingState)))
                .InSequence()
                .Returns<StateChangeContext>(ctx => ctx.NewState);

            _performer.Setup(x => x.Perform(It.IsAny<PerformContext>()))
                .InSequence();

            _stateChanger
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

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is ProcessingState && (((ProcessingState) ctx.NewState).ServerId == _context.ServerId))));
        }

        [Fact]
        public void Execute_ProcessesOnlyJobs_InEnqueuedAndProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is ProcessingState &&
                ctx.ExpectedStates.ElementAt(0) == EnqueuedState.StateName &&
                ctx.ExpectedStates.ElementAt(1) == ProcessingState.StateName)));
        }

        [Fact]
        public void Execute_DoesNotDisableFilters_DuringNormalOperation()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.DisableFilters == false)));
        }

        [Fact]
        public void Execute_DoesNotRun_PerformanceProcess_IfTransitionToProcessingStateFailed()
        {
            // Arrange
            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is ProcessingState)))
                .Returns<IState>(null);

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _performer.Verify(x => x.Perform(It.IsAny<PerformContext>()), Times.Never);
        }

        [Fact]
        public void Execute_Runs_PerformanceProcess()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _performer.Verify(x => x.Perform(It.IsNotNull<PerformContext>()));
        }

        [Fact]
        public void Execute_DoesNotMoveAJob_ToTheFailedState_ButRequeuesIt_WhenProcessThrowsOperationCanceled_DuringShutdownOnly()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            _context.StoppedTokenSource = cts;

            _performer.Setup(x => x.Perform(It.IsAny<PerformContext>()))
                .Callback(() => cts.Cancel())
                .Throws<OperationCanceledException>();

            var worker = CreateWorker();

            // Act
            Assert.Throws<OperationCanceledException>(() => worker.Execute(_context.Object));

            // Assert
            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is FailedState)),
                Times.Never);
            _fetchedJob.Verify(x => x.Requeue());
        }

        [Fact]
        public void Execute_MovesAJob_ToTheFailedState_AndNotRequeuesIt_WhenProcessThrowsOperationCanceled_WhenShutdownWasNotRequested()
        {
            // Arrange
            _performer.Setup(x => x.Perform(It.IsAny<PerformContext>()))
                .Throws<OperationCanceledException>();

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is FailedState)),
                Times.Once);
            _fetchedJob.Verify(x => x.Requeue(), Times.Never);
        }

        [Fact]
        public void Execute_DoesNotMoveAJobToFailedState_AndRemovesJobFromQueue_WhenProcessThrowsJobAbortedException()
        {
            // Arrange
            _performer.Setup(x => x.Perform(It.IsAny<PerformContext>()))
                .Throws<JobAbortedException>();

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is FailedState)),
                Times.Never);
            _fetchedJob.Verify(x => x.RemoveFromQueue());
            _fetchedJob.Verify(x => x.Requeue(), Times.Never);
        }

        [Fact]
        public void Execute_MovesJob_ToSuccessfulState_OnlyIfItIsInProcessingState()
        {
            var worker = CreateWorker();

            worker.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is SucceededState &&
                ctx.ExpectedStates.ElementAt(0) == ProcessingState.StateName)));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasInternalException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _performer
                .Setup(x => x.Perform(It.IsAny<PerformContext>()))
                .Throws(exception);

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is FailedState &&
                ((FailedState) ctx.NewState).Exception == exception &&
                ctx.DisableFilters == false)));
        }

        [Fact]
        public void Execute_MovesJob_ToFailedState_IfThereWasUserException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _performer
                .Setup(x => x.Perform(It.IsAny<PerformContext>()))
                .Throws(new JobPerformanceException("hello", exception));

            var worker = CreateWorker();

            // Act
            worker.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is FailedState &&
                ctx.DisableFilters == false)));
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
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.NewState is FailedState &&
                ctx.DisableFilters == false)));
        }

        private Worker CreateWorker(int maxStateChangeAttempts = 10)
        {
            return new Worker(_queues, _performer.Object, _stateChanger.Object, TimeSpan.FromSeconds(5), maxStateChangeAttempts);
        }

        public static void Method() { }
    }
}
