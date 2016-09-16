using System;
using System.Collections.Generic;
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

        private CoreBackgroundJobFactory CreateFactory()
        {
            return new CoreBackgroundJobFactory(_stateMachine.Object);
        }
    }
}