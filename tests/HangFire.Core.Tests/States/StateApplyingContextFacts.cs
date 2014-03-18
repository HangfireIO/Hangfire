using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateApplyingContextFacts
    {
        private const string JobId = "1";
        private readonly Mock<State> _newStateMock;
        private readonly MethodData _methodData;
        private readonly List<IStateChangedFilter> _filters;
        private readonly StateHandlerCollection _handlers;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private const string OldState = "SomeState";
        private const string NewState = "NewState";

        public StateApplyingContextFacts()
        {
            _methodData = MethodData.FromExpression(() => Console.WriteLine());
            _newStateMock = new Mock<State>();
            _newStateMock.Setup(x => x.Name).Returns(NewState);

            _transaction = new Mock<IWriteOnlyTransaction>();
            
            _filters = new List<IStateChangedFilter>();
            _handlers = new StateHandlerCollection();
        }

        [Fact]
        public void Ctor_ShouldSetPropertiesCorrectly()
        {
            var context = CreateContext();

            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newStateMock.Object, context.NewState);
            Assert.Same(_methodData, context.MethodData);
        }

        [Fact]
        public void ApplyState_ShouldThrowAnException_WhenHandlersIsNull()
        {
            var context = CreateContext();

            var exception = Assert.Throws<ArgumentNullException>(
                () => context.ApplyState(null, _filters));

            Assert.Equal("handlers", exception.ParamName);
        }

        [Fact]
        public void ApplyState_ShouldThrowAnException_WhenFiltersIsNull()
        {
            var context = CreateContext();

            var exception = Assert.Throws<ArgumentNullException>(
                () => context.ApplyState(_handlers, null));

            Assert.Equal("filters", exception.ParamName);
        }

        [Fact]
        public void ApplyState_ShouldReturnTransactionCommit()
        {
            var context = CreateContext();

            _transaction.Setup(x => x.Commit()).Returns(true);
            Assert.True(context.ApplyState(_handlers, _filters));

            _transaction.Setup(x => x.Commit()).Returns(false);
            Assert.False(context.ApplyState(_handlers, _filters));
        }

        [Fact, Sequence]
        public void ApplyState_NewState_ShouldBeCommitted()
        {
            // Arrange
            _transaction.Setup(x => x.SetJobState(
                JobId, _newStateMock.Object)).InSequence();

            _transaction.Setup(x => x.Commit()).InSequence();

            var context = CreateContext();

            // Act
            context.ApplyState(_handlers, _filters);

            // Assert - Sequence
        }

        [Fact, Sequence]
        public void ApplyState_UnapplyHandlers_ShouldBeCalled_BeforeSettingTheState()
        {
            // Arrange
            var context = CreateContext();

            var handler1 = new Mock<StateHandler>();
            handler1.Setup(x => x.StateName).Returns(OldState);
            
            var handler2 = new Mock<StateHandler>();
            handler2.Setup(x => x.StateName).Returns(OldState);

            _handlers.AddHandler(handler1.Object);
            _handlers.AddHandler(handler2.Object);

            handler1.Setup(x => x.Unapply(context, _transaction.Object)).InSequence();
            handler2.Setup(x => x.Unapply(context, _transaction.Object)).InSequence();
            _transaction.Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<State>()))
                .InSequence();

            // Act
            context.ApplyState(_handlers, _filters);

            // Assert - Sequence
        }

        [Fact, Sequence]
        public void ApplyState_ApplyHandlers_ShouldBeCalled_AfterSettingTheState()
        {
            // Arrange
            var context = CreateContext();

            var handler1 = new Mock<StateHandler>();
            handler1.Setup(x => x.StateName).Returns(NewState);

            var handler2 = new Mock<StateHandler>();
            handler2.Setup(x => x.StateName).Returns(NewState);

            _handlers.AddHandler(handler1.Object);
            _handlers.AddHandler(handler2.Object);

            _transaction
                .Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<State>()))
                .InSequence();

            handler1.Setup(x => x.Apply(context, _transaction.Object)).InSequence();
            handler2.Setup(x => x.Apply(context, _transaction.Object)).InSequence();

            // Act
            context.ApplyState(_handlers, _filters);

            // Assert - Sequence
        }

        [Fact]
        public void ApplyState_ShouldSetJobExpiration_WhenTheStateSaysToDoSo()
        {
            var context = CreateContext();
            _newStateMock.Setup(x => x.ExpireJobOnApply).Returns(true);

            context.ApplyState(_handlers, _filters);

            _transaction.Verify(x => x.ExpireJob(JobId, It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void ApplyState_ShouldPersistTheJob_WhenTheStateSaysToNotToExpireIt()
        {
            var context = CreateContext();
            _newStateMock.Setup(x => x.ExpireJobOnApply).Returns(false);

            context.ApplyState(_handlers, _filters);

            _transaction.Verify(x => x.PersistJob(JobId));
        }

        [Fact, Sequence]
        public void ApplyState_StateUnappliedFilters_ShouldBeCalled_BeforeSettingTheState()
        {
            // Arrange
            var context = CreateContext();

            var filter1 = new Mock<IStateChangedFilter>();
            var filter2 = new Mock<IStateChangedFilter>();

            _filters.Add(filter1.Object);
            _filters.Add(filter2.Object);

            filter1.Setup(x => x.OnStateUnapplied(context, _transaction.Object))
                .InSequence();
            filter2.Setup(x => x.OnStateUnapplied(context, _transaction.Object))
                .InSequence();
            _transaction
                .Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<State>()))
                .InSequence();

            // Act
            context.ApplyState(_handlers, _filters);

            // Assert - Sequence
        }

        [Fact, Sequence]
        public void ApplyState_ApplyStateFilters_ShouldBeCalled_AfterSettingTheJobState()
        {
            // Arrange
            var context = CreateContext();

            var filter1 = new Mock<IStateChangedFilter>();
            var filter2 = new Mock<IStateChangedFilter>();

            _filters.Add(filter1.Object);
            _filters.Add(filter2.Object);

            filter1.Setup(x => x.OnStateApplied(context, _transaction.Object))
                .InSequence();
            filter2.Setup(x => x.OnStateApplied(context, _transaction.Object))
                .InSequence();

            // Act
            context.ApplyState(_handlers, _filters);

            // Assert - Sequence
        }

        private StateApplyingContext CreateContext()
        {
            var connectionMock = new Mock<IStorageConnection>();
            connectionMock.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            return new StateApplyingContext(
                new StateContext(JobId, _methodData),
                connectionMock.Object,
                _newStateMock.Object,
                OldState);
        }
    }
}
