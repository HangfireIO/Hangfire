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

        private readonly Mock<IState> _state;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly ElectStateContextMock _electStateContext;
        private readonly ApplyStateContextMock _applyStateContext;
        private readonly Mock<IJobFilterProvider> _filterProvider;
        
        private readonly Mock<IStateMachine> _innerMachine;

        public StateMachineFacts()
        {
            var connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns(StateName);

            var backgroundJob = new BackgroundJobMock { Id = JobId };
            _electStateContext = new ElectStateContextMock
            {
                BackgroundJob = backgroundJob,
                CandidateStateValue = _state.Object,
                CurrentStateValue = OldStateName,
                ConnectionValue = connection
            };
            _applyStateContext = new ApplyStateContextMock
            {
                BackgroundJob = backgroundJob,
                NewState = _state,
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

        [Fact, Sequence]
        public void ElectState_CallsElectionFilters()
        {
            // Arrange
            var filter1 = CreateFilter<IElectStateFilter>();
            var filter2 = CreateFilter<IElectStateFilter>();

            filter1.Setup(x => x.OnStateElection(_electStateContext.Object))
                .InSequence();
            filter2.Setup(x => x.OnStateElection(_electStateContext.Object))
                .InSequence();

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ElectState(_electStateContext.Object);

            // Assert - Sequence
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
            stateMachine.ApplyState(_applyStateContext.Object);

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
            stateMachine.ApplyState(_applyStateContext.Object);

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