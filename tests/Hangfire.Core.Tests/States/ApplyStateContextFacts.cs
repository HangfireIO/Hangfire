using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IEnumerable<IState> _traversedStates = Enumerable.Empty<IState>();
        private readonly Mock<JobStorage> _storage;
        private readonly BackgroundJobMock _backgroundJob;

        public ApplyStateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _backgroundJob = new BackgroundJobMock();
            _newState = new Mock<IState>();
            _newState.Setup(x => x.Name).Returns(NewState);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(null, _backgroundJob.Object, _newState.Object, OldState, _traversedStates));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenBackgroundJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_storage.Object, null, _newState.Object, OldState, _traversedStates));

            Assert.Equal("backgroundJob", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_storage.Object, _backgroundJob.Object, null, OldState, _traversedStates));

            Assert.Equal("newState", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTraversedStatesIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_storage.Object, _backgroundJob.Object, _newState.Object, OldState, null));

            Assert.Equal("traversedStates", exception.ParamName);
        }

        [Fact]
        public void Ctor_ShouldSetPropertiesCorrectly()
        {
            var context = new ApplyStateContext(
                _storage.Object,
                _backgroundJob.Object,
                _newState.Object,
                OldState,
                _traversedStates);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_backgroundJob.Object, context.BackgroundJob);
            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Same(_traversedStates, context.TraversedStates);
        }
    }
}
