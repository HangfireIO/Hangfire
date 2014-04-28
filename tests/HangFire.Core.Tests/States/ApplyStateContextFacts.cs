using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ApplyStateContextFacts
    {
        private const string JobId = "1";
        private const string OldState = "SomeState";
        private const string NewState = "NewState";

        private readonly Mock<State> _newState;
        private readonly Job _job;
        private readonly List<IApplyStateFilter> _filters;
        private readonly StateHandlerCollection _handlers;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly StateContext _stateContext;
        private readonly Mock<IStorageConnection> _connection;

        public ApplyStateContextFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine());
            _newState = new Mock<State>();
            _newState.Setup(x => x.Name).Returns(NewState);

            _transaction = new Mock<IWriteOnlyTransaction>();
            
            _filters = new List<IApplyStateFilter>();
            _handlers = new StateHandlerCollection();

            _stateContext = new StateContext(JobId, _job);
            _connection = new Mock<IStorageConnection>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(null, _stateContext, _newState.Object, OldState));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_connection.Object, _stateContext, null, OldState));

            Assert.Equal("newState", exception.ParamName);
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

        [Fact, Sequence]
        public void ApplyState_NewState_ShouldBeCommitted()
        {
            // Arrange
            _transaction.Setup(x => x.SetJobState(
                JobId, _newState.Object)).InSequence();

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

            var handler1 = new Mock<IStateHandler>();
            handler1.Setup(x => x.StateName).Returns(OldState);

            var handler2 = new Mock<IStateHandler>();
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

            var handler1 = new Mock<IStateHandler>();
            handler1.Setup(x => x.StateName).Returns(NewState);

            var handler2 = new Mock<IStateHandler>();
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
            _newState.Setup(x => x.ExpireJobOnApply).Returns(true);

            context.ApplyState(_handlers, _filters);

            _transaction.Verify(x => x.ExpireJob(JobId, It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void ApplyState_ShouldPersistTheJob_WhenTheStateSaysToNotToExpireIt()
        {
            var context = CreateContext();
            _newState.Setup(x => x.ExpireJobOnApply).Returns(false);

            context.ApplyState(_handlers, _filters);

            _transaction.Verify(x => x.PersistJob(JobId));
        }

        [Fact, Sequence]
        public void ApplyState_StateUnappliedFilters_ShouldBeCalled_BeforeSettingTheState()
        {
            // Arrange
            var context = CreateContext();

            var filter1 = new Mock<IApplyStateFilter>();
            var filter2 = new Mock<IApplyStateFilter>();

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

            var filter1 = new Mock<IApplyStateFilter>();
            var filter2 = new Mock<IApplyStateFilter>();

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

        [Fact]
        public void Ctor_ShouldSetPropertiesCorrectly()
        {
            var context = CreateContext();

            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Same(_job, context.Job);
        }

        private ApplyStateContext CreateContext()
        {
            return new ApplyStateContext(
                _connection.Object,
                _stateContext,
                _newState.Object,
                OldState);
        }
    }
}
