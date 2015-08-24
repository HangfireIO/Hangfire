using System;
using System.Collections.Generic;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class CoreJobCreationProcessFacts
    {
        private const string JobId = "jobId";
        private readonly Mock<IStateChangeProcess> _stateMachine;
        private readonly CreateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public CoreJobCreationProcessFacts()
        {
            _stateMachine = new Mock<IStateChangeProcess>();
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
                () => new CoreJobCreationProcess(null));

            Assert.Equal("stateMachine", exception.ParamName);
        }
        
        [Fact]
        public void CreateJob_CreatesExpiredJob()
        {
            _context.Object.Parameters.Add("Name", "Value");

            var process = CreateProcess();

            process.Run(_context.Object);

            _context.Connection.Verify(x => x.CreateExpiredJob(
                _context.Job,
                It.Is<Dictionary<string, string>>(d => d["Name"] == "\"Value\""),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void CreateJob_ChangesTheStateOfACreatedJob()
        {
            var process = CreateProcess();

            process.Run(_context.Object);

            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(
                    sc => sc.BackgroundJob.Id == JobId && sc.BackgroundJob.Job == _context.Job
                    && sc.NewState == _context.InitialState.Object && sc.OldStateName == null)));

            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void CreateJob_ReturnsNewJobId()
        {
            var process = CreateProcess();
            Assert.Equal(JobId, process.Run(_context.Object));
        }

        private CoreJobCreationProcess CreateProcess()
        {
            return new CoreJobCreationProcess(_stateMachine.Object);
        }
    }
}