using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly StateChangingContext _stateChangingContext;
        private readonly Mock<State> _newStateMock;
        private readonly MethodData _methodData;
        private readonly IEnumerable<IStateChangedFilter> _filters;
        private readonly Dictionary<string, List<StateHandler>> _handlers;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private const string OldState = "SomeState";
        private const string NewState = "NewState";

        public StateApplyingContextFacts()
        {
            _methodData = MethodData.FromExpression(() => Console.WriteLine());
            _newStateMock = new Mock<State>();
            _newStateMock.Setup(x => x.StateName).Returns(NewState);

            var connectionMock = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            connectionMock.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _stateChangingContext = new StateChangingContext(
                new StateContext(JobId, _methodData), 
                _newStateMock.Object, 
                OldState, 
                connectionMock.Object);

            _filters = Enumerable.Empty<IStateChangedFilter>();
            _handlers = new Dictionary<string, List<StateHandler>>();
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

        [Fact, Sequence]
        public void ApplyState_NewState_ShouldBeCommitted()
        {
            _transaction.Setup(x => x.SetJobState(
                JobId, _newStateMock.Object, _methodData)).InSequence();

            _transaction.Setup(x => x.Commit()).InSequence();

            var context = CreateContext();
            context.ApplyState(_handlers, _filters);
        }

        /*[Fact, Sequence]
        public void ApplyState_UnapplyHandlers_ShouldBeCalled_BeforeSettingTheState()
        {
            var context = CreateContext();

            var handler1 = new Mock<StateHandler>();
            handler1.Setup(x => x.StateName).Returns(OldState);

            var handler2 = new Mock<StateHandler>();
            handler2.Setup(x => x.StateName).Returns(OldState);

            handler1.Setup(x => x.Unapply(context, _transaction.Object)).InSequence();
            handler2.Setup(x => x.Unapply(context, _transaction.Object)).InSequence();
            _transaction.Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<State>(), It.IsAny<MethodData>()))
                .InSequence();

            context.ApplyState()
        }*/

        private StateApplyingContext CreateContext()
        {
            return new StateApplyingContext(_stateChangingContext);
        }
    }
}
