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
        private const string JobId = "1";
        private const string OldState = "SomeState";
        private const string NewState = "NewState";

        private readonly Mock<IState> _newState;
        private readonly StateContextMock _stateContext;
        private readonly Job _job;
        private readonly IEnumerable<IState> _traversedStates = Enumerable.Empty<IState>();

        public ApplyStateContextFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine());

            _stateContext = new StateContextMock
            {
                JobIdValue = JobId, 
                JobValue = _job,
            };

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
            Assert.Same(_job, context.Job);
            Assert.Same(_traversedStates, context.TraversedStates);
        }
    }
}
