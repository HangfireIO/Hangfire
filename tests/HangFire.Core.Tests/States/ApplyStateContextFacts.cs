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

        private readonly Mock<IState> _newState;
        private readonly List<IApplyStateFilter> _filters;
        private readonly StateHandlerCollection _handlers;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly StateContextMock _stateContext;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Job _job;

        public ApplyStateContextFacts()
        {
            _connection = new Mock<IStorageConnection>();

            _job = Job.FromExpression(() => Console.WriteLine());

            _stateContext = new StateContextMock();
            _stateContext.JobIdValue = JobId;
            _stateContext.JobValue = _job;
            _stateContext.ConnectionValue = _connection;

            _newState = new Mock<IState>();
            _newState.Setup(x => x.Name).Returns(NewState);

            _filters = new List<IApplyStateFilter>();
            _handlers = new StateHandlerCollection();

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_stateContext.Object, null, OldState));

            Assert.Equal("newState", exception.ParamName);
        }

        [Fact]
        public void Ctor_ShouldSetPropertiesCorrectly()
        {
            var context = new ApplyStateContext(
                _stateContext.Object,
                _newState.Object,
                OldState);

            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Same(_job, context.Job);
        }
    }
}
