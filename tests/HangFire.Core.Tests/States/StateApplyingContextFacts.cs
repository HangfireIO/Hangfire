using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateApplyingContextFacts
    {
        private const string JobId = "1";
        private StateChangingContext _stateChangingContext;
        private Mock<IStorageConnection> _connectionMock;
        private Mock<State> _stateMock;

        public StateApplyingContextFacts()
        {
            var methodInfo = typeof(SucceededStateHandlerFacts)
                .GetMethod("TestMethod");
            var jobMethod = new JobMethod(typeof(SucceededStateHandlerFacts), methodInfo);

            var stateContext = new StateContext(JobId, jobMethod);
            _stateMock = new Mock<State>();
            _connectionMock = new Mock<IStorageConnection>();
            _stateChangingContext = new StateChangingContext(
                stateContext, _stateMock.Object, "Old", _connectionMock.Object);
        }

        public void Ctor_ShouldThrowAnException_WhenGivenContextIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StateApplyingContext(null));
        }
    }
}
