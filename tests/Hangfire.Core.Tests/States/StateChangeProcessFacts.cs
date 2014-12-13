using System;
using System.Collections.Generic;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateChangeProcessFacts
    {
        private const string OldStateName = "OldState";
        private const string StateName = "State";
        private const string JobId = "job";

        private readonly StateHandlerCollection _handlers = new StateHandlerCollection();
        private readonly List<object> _filters = new List<object>();

        private readonly Mock<IState> _state;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly ElectStateContextMock _electStateContext;
        private readonly ApplyStateContextMock _applyStateContext;

        public StateChangeProcessFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns(StateName);

            var context = new StateContextMock { JobIdValue = JobId };
            _electStateContext = new ElectStateContextMock
            {
                StateContextValue = context,
                CandidateStateValue = _state.Object,
                CurrentStateValue = OldStateName
            };
            _applyStateContext = new ApplyStateContextMock
            {
                StateContextValue = context,
                NewStateValue = _state.Object,
                OldStateValue = OldStateName
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenHandlersCollectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeProcess(null, _filters));

            Assert.Equal("handlers", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFiltersCollectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeProcess(_handlers, null));

            Assert.Equal("filters", exception.ParamName);
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

            var process = CreateProcess();

            // Act
            process.ElectState(_connection.Object, _electStateContext.Object);

            // Assert - Sequence
        }

        [Fact, Sequence]
        public void ApplyState_CallsUnapplyHandlers_BeforeSettingTheState()
        {
            // Arrange
            var handler1 = CreateStateHandler(OldStateName);
            var handler2 = CreateStateHandler(OldStateName);

            handler1
                .Setup(x => x.Unapply(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();

            handler2
                .Setup(x => x.Unapply(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();

            _transaction.Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<IState>()))
                .InSequence();

            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            // Assert - Sequence
        }

        [Fact]
        public void ApplyState_DoesNotCallUnapplyHandlers_ForDifferentStates()
        {
            // Arrange
            var handler = CreateStateHandler(StateName);
            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            // Assert
            handler.Verify(
                x => x.Unapply(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()),
                Times.Never);
        }

        [Fact, Sequence]
        public void ApplyState_ShouldCallApplyHandlers_AfterSettingTheState()
        {
            // Arrange
            var handler1 = CreateStateHandler(StateName);
            var handler2 = CreateStateHandler(StateName);

            _transaction
                .Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<IState>()))
                .InSequence();

            handler1.Setup(x => x.Apply(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();
            handler2.Setup(x => x.Apply(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();

            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            // Assert - Sequence
        }

        [Fact]
        public void ApplyState_DoesNotCallApplyHandlers_ForDifferentStates()
        {
            // Arrange
            var handler = CreateStateHandler(OldStateName);
            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            // Assert
            handler.Verify(
                x => x.Apply(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()),
                Times.Never);
        }

        [Fact]
        public void ApplyState_SetsJobExpiration_IfStateIsFinal()
        {
            _state.Setup(x => x.IsFinal).Returns(true);
            var process = CreateProcess();

            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            _transaction.Verify(x => x.ExpireJob(JobId, It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void ApplyState_PersistTheJob_IfStateIsNotFinal()
        {
            _state.Setup(x => x.IsFinal).Returns(false);
            var process = CreateProcess();

            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            _transaction.Verify(x => x.PersistJob(JobId));
        }

        [Fact, Sequence]
        public void ApplyState_CallsStateUnappliedFilters_BeforeSettingTheState()
        {
            // Arrange
            var filter1 = CreateFilter<IApplyStateFilter>();
            var filter2 = CreateFilter<IApplyStateFilter>();

            filter1.Setup(x => x.OnStateUnapplied(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();
            filter2.Setup(x => x.OnStateUnapplied(It.IsNotNull<ApplyStateContext>(), _transaction.Object))
                .InSequence();
            _transaction
                .Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<IState>()))
                .InSequence();

            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

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

            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            // Assert - Sequence
        }
        
        [Fact]
        public void ApplyState_AddsJobHistory_ForTraversedStates()
        {
            // Arrange
            _applyStateContext.TraversedStatesValue = new[] { _state.Object };

            var process = CreateProcess();

            // Act
            process.ApplyState(_transaction.Object, _applyStateContext.Object, true);

            // Assert
            _transaction.Verify(x => x.AddJobState(JobId, _state.Object));
        }

        private StateChangeProcess CreateProcess()
        {
            return new StateChangeProcess(_handlers, _filters);
        }

        private Mock<IStateHandler> CreateStateHandler(string stateName)
        {
            var handler = new Mock<IStateHandler>();
            handler.Setup(x => x.StateName).Returns(stateName);

            _handlers.AddHandler(handler.Object);
            return handler;
        }

        private Mock<T> CreateFilter<T>() where T : class
        {
            var filter = new Mock<T>();
            _filters.Add(filter.Object);

            return filter;
        }
    }
}