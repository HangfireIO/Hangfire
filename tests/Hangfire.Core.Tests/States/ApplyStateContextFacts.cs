using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ApplyStateContextFacts
    {
        private const string OldState = "SomeState";
        private const string NewState = "NewState";

        private readonly Mock<IState> _newState;
        private readonly StateContextMock _stateContext;
        private readonly IEnumerable<IState> _traversedStates = Enumerable.Empty<IState>();

        public ApplyStateContextFacts()
        {
            _stateContext = new StateContextMock();

            _newState = new Mock<IState>();
            _newState.Setup(x => x.Name).Returns(NewState);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_stateContext.Object, null, OldState, _traversedStates));

            Assert.Equal("newState", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTraversedStatesIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_stateContext.Object, _newState.Object, OldState, null));

            Assert.Equal("traversedStates", exception.ParamName);
        }

        [Fact]
        public void Ctor_ShouldSetPropertiesCorrectly()
        {
            var context = new ApplyStateContext(
                _stateContext.Object,
                _newState.Object,
                OldState,
                _traversedStates);

            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Same(_stateContext.BackgroundJob.Object, context.BackgroundJob);
            Assert.Same(_traversedStates, context.TraversedStates);
        }
    }
}
