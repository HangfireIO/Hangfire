using System;
using System.Net.Configuration;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Server;
using HangFire.Server.Performing;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ProcessingJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";
        private const string Server = "server";

        private readonly Mock<IStorageConnection> _connection;
        private readonly WorkerContext _context;
        private readonly Mock<IJobPerformanceProcess> _performanceProcess;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStateMachineFactory> _stateMachineFactory;

        public ProcessingJobFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _context = new WorkerContext(Server, new string[0], 1);
            _performanceProcess = new Mock<IJobPerformanceProcess>();

            _stateMachine = new Mock<IStateMachine>();

            _stateMachineFactory = new Mock<IStateMachineFactory>();
            _stateMachineFactory.Setup(x => x.Create(It.IsNotNull<IStorageConnection>()))
                .Returns(_stateMachine.Object);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    Job = Job.FromExpression(() => Method()),
                });

            _stateMachine.Setup(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<State>(),
                It.IsAny<string[]>())).Returns(true);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ProcessingJob(null, _stateMachineFactory.Object, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ProcessingJob(_connection.Object, null, JobId, Queue));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ProcessingJob(_connection.Object, _stateMachineFactory.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ProcessingJob(_connection.Object, _stateMachineFactory.Object, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var processingJob = CreateProcessingJob();

            Assert.Equal(JobId, processingJob.JobId);
            Assert.Equal(Queue, processingJob.Queue);
        }

        [Fact]
        public void Process_ThrowsAnException_WhenContextIsNull()
        {
            var processingJob = CreateProcessingJob();

            var exception = Assert.Throws<ArgumentNullException>(
                () => processingJob.Process(null, _performanceProcess.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Process_ThrowsAnException_WhenProcessIsNull()
        {
            var processingJob = CreateProcessingJob();

            var exception = Assert.Throws<ArgumentNullException>(
                () => processingJob.Process(_context, null));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact, Sequence]
        public void Process_ExecutesDefaultWorkflow_WhenJobIsCorrect()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.TryToChangeState(JobId, It.IsAny<ProcessingState>(), It.IsAny<string[]>()))
                .InSequence()
                .Returns(true);

            _performanceProcess.Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .InSequence();

            _stateMachine
                .Setup(x => x.TryToChangeState(JobId, It.IsAny<SucceededState>(), It.IsAny<string[]>()))
                .InSequence()
                .Returns(true);

            var processingJob = CreateProcessingJob();

            // Act
            processingJob.Process(_context, _performanceProcess.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Process_SetsCurrentServer_ToProcessingState()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _stateMachine.Verify(x => x.TryToChangeState(
                It.IsAny<string>(), 
                It.Is<ProcessingState>(state => state.ServerName == Server),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Process_ProcessesOnlyJobs_InEnqueuedAndProcessingState()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _stateMachine.Verify(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<ProcessingState>(),
                It.Is<string[]>(
                    states => states.Length == 2 &&
                        states[0] == EnqueuedState.StateName && states[1] == ProcessingState.StateName)));
        }

        [Fact]
        public void Process_DoesNotRun_PerformanceProcess_IfTransitionToProcessingStateFailed()
        {
            _stateMachine
                .Setup(x => x.TryToChangeState(
                    It.IsAny<string>(),
                    It.IsAny<ProcessingState>(),
                    It.IsAny<string[]>()))
                .Returns(false);

            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _performanceProcess.Verify(
                x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()),
                Times.Never);
        }

        [Fact]
        public void Process_Runs_PerformanceProcess()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _performanceProcess.Verify(x => x.Run(
                It.IsNotNull<PerformContext>(),
                It.IsNotNull<IJobPerformer>()));
        }

        [Fact]
        public void Process_MovesJob_ToSuccessfulState_OnlyIfItIsInProcessingState()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _stateMachine.Verify(x => x.TryToChangeState(
                It.IsAny<string>(),
                It.IsAny<SucceededState>(),
                It.Is<string[]>(states => states.Length == 1 && states[0] == ProcessingState.StateName)));
        }

        [Fact]
        public void Process_MovesJob_ToFailedState_IfThereWasInternalException()
        {
            var exception = new InvalidOperationException();
            _performanceProcess
                .Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .Throws(exception);

            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _stateMachine.Verify(x => x.TryToChangeState(
                JobId, 
                It.Is<FailedState>(state => state.Exception == exception && state.Reason.Contains("Internal")),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Process_MovesJob_ToFailedState_IfThereWasUserException()
        {
            var exception = new InvalidOperationException();
            _performanceProcess
                .Setup(x => x.Run(It.IsAny<PerformContext>(), It.IsAny<IJobPerformer>()))
                .Throws(new JobPerformanceException("hello", exception));

            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _stateMachine.Verify(x => x.TryToChangeState(
                JobId,
                It.Is<FailedState>(state => state.Exception == exception && state.Reason == "hello"),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Process_MovesJob_ToFailedState_IfThereWasJobLoadException()
        {
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { LoadException = new JobLoadException() });

            var processingJob = CreateProcessingJob();

            processingJob.Process(_context, _performanceProcess.Object);

            _stateMachine.Verify(x => x.TryToChangeState(
                JobId,
                It.IsAny<FailedState>(),
                It.IsAny<string[]>()));
        }

        [Fact]
        public void Dispose_DeletesJobFromTheQueue()
        {
            var processingJob = CreateProcessingJob();
            processingJob.Dispose();

            _connection.Verify(x => x.DeleteJobFromQueue(JobId, Queue));
        }

        private ProcessingJob CreateProcessingJob()
        {
            return new ProcessingJob(_connection.Object, _stateMachineFactory.Object, JobId, Queue);
        }

        public static void Method() { }
    }
}
