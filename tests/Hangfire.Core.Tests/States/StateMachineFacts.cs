using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateMachineFacts
    {
        private const string OldStateName = "OldState";
        private const string StateName = "State";
        private const string JobId = "job";

        private readonly List<object> _filters = new List<object>();
        
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly ApplyStateContextMock _context;
        private readonly Mock<IJobFilterProvider> _filterProvider;
        
        private readonly Mock<IStateMachine> _innerMachine;

        public StateMachineFacts()
        {
            var connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            var backgroundJob = new BackgroundJobMock { Id = JobId };
            _context = new ApplyStateContextMock
            {
                BackgroundJob = backgroundJob,
                OldStateName = OldStateName,
                Transaction = _transaction
            };

            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsNotNull<Job>())).Returns(
                _filters.Select(f => new JobFilter(f, JobFilterScope.Type, null)));
            
            _innerMachine = new Mock<IStateMachine>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProviderIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(null, _innerMachine.Object));

            Assert.Equal("filterProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerStateMachineIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(_filterProvider.Object, null));

            Assert.Equal("innerStateMachine", exception.ParamName);
        }

        [Fact]
        public void ApplyState_CallsElectionFilterWithCorrectProperties()
        {
            // Arrange
            var filter = CreateFilter<IElectStateFilter>();
            
            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_context.Object);

            filter.Verify(x => x.OnStateElection(It.Is<ElectStateContext>(context =>
                context.Storage == _context.Storage.Object &&
                context.Connection == _context.Connection.Object &&
                context.BackgroundJob == _context.BackgroundJob.Object &&
                context.CandidateState == _context.NewState.Object &&
                context.CurrentState == _context.OldStateName)));
        }

        [Fact, Sequence]
        public void ApplyState_CallsElectionFilters()
        {
            // Arrange
            var filter1 = CreateFilter<IElectStateFilter>();
            var filter2 = CreateFilter<IElectStateFilter>();

            filter1.Setup(x => x.OnStateElection(It.IsAny<ElectStateContext>()))
                .InSequence();
            filter2.Setup(x => x.OnStateElection(It.IsAny<ElectStateContext>()))
                .InSequence();

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_context.Object);

            // Assert - Sequence
        }

        [Fact]
        public void ApplyState_AddsJobHistory_ForTraversedStates()
        {
            // Arrange
            var anotherState = new Mock<IState>();
            var filter = CreateFilter<IElectStateFilter>();
            filter.Setup(x => x.OnStateElection(It.IsNotNull<ElectStateContext>()))
                .Callback<ElectStateContext>(context => context.CandidateState = anotherState.Object);

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_context.Object);

            // Assert
            _context.Transaction.Verify(x => x.AddJobState(JobId, _context.NewState.Object));
        }

        [Fact, Sequence]
        public void ApplyState_CallsStateUnappliedFilters_BeforeCallingInnerStateMachine()
        {
            // Arrange
            var filter1 = CreateFilter<IApplyStateFilter>();
            var filter2 = CreateFilter<IApplyStateFilter>();

            filter1.Setup(x => x.OnStateUnapplied(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();
            filter2.Setup(x => x.OnStateUnapplied(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();
            _innerMachine
                .Setup(x => x.ApplyState(It.IsAny<ApplyStateContext>()))
                .InSequence();

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_context.Object);

            // Assert - Sequence
        }

        [Fact, Sequence]
        public void ApplyState_CallsStateAppliedFilters_AfterSettingTheState()
        {
            // Arrange
            var filter1 = CreateFilter<IApplyStateFilter>();
            var filter2 = CreateFilter<IApplyStateFilter>();

            filter1.Setup(x => x.OnStateApplied(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();
            filter2.Setup(x => x.OnStateApplied(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_context.Object);

            // Assert - Sequence
        }
        
        private StateMachine CreateStateMachine()
        {
            return new StateMachine(_filterProvider.Object, _innerMachine.Object);
        }

        private Mock<T> CreateFilter<T>() where T : class
        {
            var filter = new Mock<T>();
            _filters.Add(filter.Object);

            return filter;
        }
    }
}