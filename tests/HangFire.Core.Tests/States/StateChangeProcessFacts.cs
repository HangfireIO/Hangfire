using System;
using System.Collections.Generic;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateChangeProcessFacts
    {
        private const string OldStateName = "OldState";
        private const string StateName = "State";
        private const string JobId = "job";

        private readonly StateHandlerCollection _handlers = new StateHandlerCollection();
        private readonly List<object> _filters = new List<object>();

        private readonly StateContextMock _context;
        private readonly Mock<IState> _state;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public StateChangeProcessFacts()
        {
            var connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _context = new StateContextMock();
            _context.JobIdValue = JobId;
            _context.ConnectionValue = connection;

            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns(StateName);
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
        public void ChangeState_CommitsTheNewState_AndReturnsTrue()
        {
            // Arrange
            _transaction.Setup(x => x.SetJobState(JobId, _state.Object)).InSequence();
            _transaction.Setup(x => x.Commit()).InSequence();

            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert - Sequence
            Assert.True(result);
        }

        [Fact, Sequence]
        public void ChangeState_CallsUnapplyHandlers_BeforeSettingTheState()
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
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert - Sequence
        }

        [Fact]
        public void ChangeState_DoesNotCallUnapplyHandlers_ForDifferentStates()
        {
            // Arrange
            var handler = CreateStateHandler(StateName);
            var process = CreateProcess();

            // Act
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert
            handler.Verify(
                x => x.Unapply(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()),
                Times.Never);
        }

        [Fact, Sequence]
        public void ChangeState_ShouldCallApplyHandlers_AfterSettingTheState()
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
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert - Sequence
        }

        [Fact]
        public void ChangeState_DoesNotCallApplyHandlers_ForDifferentStates()
        {
            // Arrange
            var handler = CreateStateHandler(OldStateName);
            var process = CreateProcess();

            // Act
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert
            handler.Verify(
                x => x.Apply(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_SetsJobExpiration_IfStateIsFinal()
        {
            _state.Setup(x => x.IsFinal).Returns(true);
            var process = CreateProcess();

            process.ChangeState(_context.Object, _state.Object, OldStateName);

            _transaction.Verify(x => x.ExpireJob(JobId, It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void ChangeState_PersistTheJob_IfStateIsNotFinal()
        {
            _state.Setup(x => x.IsFinal).Returns(false);
            var process = CreateProcess();

            process.ChangeState(_context.Object, _state.Object, OldStateName);

            _transaction.Verify(x => x.PersistJob(JobId));
        }

        [Fact, Sequence]
        public void ChangeState_CallsStateUnappliedFilters_BeforeSettingTheState()
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
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert - Sequence
        }

        [Fact, Sequence]
        public void ChangeState_CallsStateAppliedFilters_AfterSettingTheState()
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
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert - Sequence
        }

        [Fact]
        public void ChangeState_SetsAnotherState_WhenItWasElected()
        {
            // Arrange
            var anotherState = new Mock<IState>();
            var filter = CreateFilter<IElectStateFilter>();

            filter.Setup(x => x.OnStateElection(It.IsNotNull<ElectStateContext>()))
                .Callback((ElectStateContext context) => context.CandidateState = anotherState.Object);

            var process = CreateProcess();

            // Act
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert - Sequence
            _transaction.Verify(x => x.SetJobState(JobId, anotherState.Object));
        }

        [Fact]
        public void ChangeState_AddsJobHistory_WhenAFilterChangesCandidateState()
        {
            // Arrange
            var newState = new Mock<IState>();
            var filter = CreateFilter<IElectStateFilter>();

            filter.Setup(x => x.OnStateElection(It.IsNotNull<ElectStateContext>()))
                .Callback((ElectStateContext x) => x.CandidateState = newState.Object);

            var process = CreateProcess();

            // Act
            process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert
            _transaction.Verify(x => x.AddJobState(JobId, _state.Object));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void ChangeState_AppliesFailedState_WhenThereIsAnException_AndReturnsFalse()
        {
            // Arrange
            var exception = new NotSupportedException();
            var filter = CreateFilter<IApplyStateFilter>();

            filter.Setup(x => x.OnStateApplied(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()))
                .Throws(exception);

            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object, _state.Object, OldStateName);

            // Assert
            _transaction.Verify(x => x.SetJobState(
                JobId, 
                It.Is<FailedState>(s => s.Exception == exception)));

            Assert.False(result);
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