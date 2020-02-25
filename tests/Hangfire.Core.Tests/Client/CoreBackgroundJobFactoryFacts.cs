using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Client
{
    public class CoreBackgroundJobFactoryFacts
    {
        private const string JobId = "jobId";
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly CreateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public CoreBackgroundJobFactoryFacts()
        {
            _stateMachine = new Mock<IStateMachine>();
            _context = new CreateContextMock();
            _transaction = new Mock<IWriteOnlyTransaction>();

            _context.Connection.Setup(x => x.CreateExpiredJob(
                It.IsAny<Job>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>())).Returns(JobId);
            _context.Connection.Setup(x => x.GetJobData(JobId)).Returns(new JobData());
            _context.Connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CoreBackgroundJobFactory(null));

            Assert.Equal("stateMachine", exception.ParamName);
        }

        [Fact]
        public void Create_ReturnsNull_WhenCreateExpiredJobReturnedNull()
        {
            _context.Connection
                .Setup(x => x.CreateExpiredJob(It.IsAny<Job>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                .Returns<string>(null);

            var factory = CreateFactory();
            var result = factory.Create(_context.Object);

            Assert.Null(result);
        }
        
        [Fact]
        public void CreateJob_CreatesExpiredJob()
        {
            _context.Object.Parameters.Add("Name", "Value");

            var factory = CreateFactory();

            factory.Create(_context.Object);

            _context.Connection.Verify(x => x.CreateExpiredJob(
                _context.Job,
                It.Is<Dictionary<string, string>>(d => d["Name"] == "\"Value\""),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void CreateJob_ChangesTheStateOfACreatedJob()
        {
            var factory = CreateFactory();

            factory.Create(_context.Object);

            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(
                    sc => sc.BackgroundJob.Id == JobId && sc.BackgroundJob.Job == _context.Job
                    && sc.NewState == _context.InitialState.Object && sc.OldStateName == null)));

            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void CreateJob_ReturnsNewJobId()
        {
            var factory = CreateFactory();
            Assert.Equal(JobId, factory.Create(_context.Object).Id);
        }

        [Fact]
        public void Create_DoesNotRetryCreateExpiredJobMethod_ByDefault_AndThrowsAnException()
        {
            // Arrange
            _context.Connection
                .Setup(x => x.CreateExpiredJob(It.IsAny<Job>(), It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                .Throws<InvalidOperationException>();

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(() => factory.Create(_context.Object));

            // Assert
            _context.Connection.Verify(
                x => x.CreateExpiredJob(It.IsAny<Job>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public void Create_DoesNotRetryStateTransaction_ByDefault_AndThrowsAnException()
        {
            // Arrange
            _stateMachine.Setup(x => x.ApplyState(It.IsAny<ApplyStateContext>())).Throws<InvalidOperationException>();

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(() => factory.Create(_context.Object));

            // Assert
            _context.Connection.Verify(
                x => x.CreateExpiredJob(It.IsAny<Job>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()),
                Times.Once);
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Once);
            _context.Connection.Verify(x => x.GetJobData(It.IsAny<string>()), Times.Never);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void Create_IsResilientToASingleCreateExpiredJobFault_WhenRetriesEnabled()
        {
            // Arrange
            _context.Connection
                .SetupSequence(x => x.CreateExpiredJob(It.IsAny<Job>(), It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                .Throws<InvalidOperationException>()
                .Returns(JobId);

            var factory = CreateFactory(retries: 1);

            // Act
            factory.Create(_context.Object);

            // Assert
            _context.Connection.Verify(
                x => x.CreateExpiredJob(It.IsNotNull<Job>(), It.IsNotNull<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()),
                Times.Exactly(2));
            _stateMachine.Verify(x => x.ApplyState(It.IsNotNull<ApplyStateContext>()), Times.Once);
            _context.Connection.Verify(x => x.GetJobData(It.IsAny<string>()), Times.Never);
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public void Create_IsResilientToASingleStateMachineFault_WhenRetriesEnabled()
        {
            // Arrange
            _stateMachine.SetupSequence(x => x.ApplyState(It.IsAny<ApplyStateContext>()))
                .Throws<InvalidOperationException>()
                .Returns(_context.InitialState.Object);

            var factory = CreateFactory(retries: 1);

            // Act
            factory.Create(_context.Object);

            // Assert
            _context.Connection.Verify(
                x => x.CreateExpiredJob(It.IsNotNull<Job>(), It.IsNotNull<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()),
                Times.Once);
            _stateMachine.Verify(x => x.ApplyState(It.IsNotNull<ApplyStateContext>()), Times.Exactly(2));
            _context.Connection.Verify(x => x.GetJobData(JobId), Times.Once);
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public void Create_DoesNotInitializeJobTwice_WhenTransactionFaulted_WhenRetriesEnabled()
        {
            // Arrange
            _transaction.SetupSequence(x => x.Commit())
                .Throws<TimeoutException>()
                .Pass();

            _context.Connection.Setup(x => x.GetJobData(JobId)).Returns(new JobData
            {
                State = EnqueuedState.StateName
            });

            var factory = CreateFactory(retries: 1);

            // Act
            factory.Create(_context.Object);

            // Assert
            _context.Connection.Verify(
                x => x.CreateExpiredJob(It.IsNotNull<Job>(), It.IsNotNull<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()),
                Times.Once);
            _stateMachine.Verify(x => x.ApplyState(It.IsNotNull<ApplyStateContext>()), Times.Once);
            _context.Connection.Verify(x => x.GetJobData(JobId), Times.Once);
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public void Create_ThrowsAnException_WhenJobDataReturnsNull_OnStateTransactionRetry()
        {
            // Arrange
            _transaction.SetupSequence(x => x.Commit())
                .Throws<TimeoutException>()
                .Pass();

            _context.Connection.Setup(x => x.GetJobData(It.IsAny<string>())).Returns((JobData)null);

            var factory = CreateFactory(retries: 1);

            // Act
            var exception = Assert.Throws<AggregateException>(() => factory.Create(_context.Object));

            // Assert
            Assert.IsType<TimeoutException>(exception.InnerExceptions[0]);
            Assert.IsType<InvalidOperationException>(exception.InnerExceptions[1]);

            _context.Connection.Verify(
                x => x.CreateExpiredJob(It.IsNotNull<Job>(), It.IsNotNull<IDictionary<string, string>>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()),
                Times.Once);
            _stateMachine.Verify(x => x.ApplyState(It.IsNotNull<ApplyStateContext>()), Times.Once);
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public void Create_ThrowsAnException_AndLeavesJobUninitialized_WhenAllRetryAttemptsExhausted_WhenCallingCreateExpiredJob()
        {
            // Arrange
            _context.Connection
                .SetupSequence(x => x.CreateExpiredJob(It.IsAny<Job>(), It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                .Throws<NotSupportedException>()
                .Throws<TimeoutException>();

            var factory = CreateFactory(retries: 1);

            // Act
            var exception = Assert.Throws<AggregateException>(() => factory.Create(_context.Object));

            // Assert
            Assert.IsType<NotSupportedException>(exception.InnerExceptions[0]);
            Assert.IsType<TimeoutException>(exception.InnerExceptions[1]);

            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void Create_ThrowsAnException_AndLeavesJobAsIs_WhenAllRetryAttemptsExhausted_WithFaultyStateTransaction()
        {
            // Arrange
            _stateMachine.SetupSequence(x => x.ApplyState(It.IsAny<ApplyStateContext>()))
                .Throws<NotSupportedException>()
                .Returns(_context.InitialState.Object);

            _transaction.SetupSequence(x => x.Commit())
                .Throws<TimeoutException>()
                .Pass();

            var factory = CreateFactory(retries: 1);

            // Act
            var exception = Assert.Throws<AggregateException>(() => factory.Create(_context.Object));

            // Assert
            Assert.IsType<NotSupportedException>(exception.InnerExceptions[0]);
            Assert.IsType<TimeoutException>(exception.InnerExceptions[1]);

            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Exactly(2));
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        private CoreBackgroundJobFactory CreateFactory(int? retries = null)
        {
            var factory = new CoreBackgroundJobFactory(_stateMachine.Object);
            if (retries.HasValue) factory.RetryAttempts = retries.Value;

            return factory;
        }
    }
}