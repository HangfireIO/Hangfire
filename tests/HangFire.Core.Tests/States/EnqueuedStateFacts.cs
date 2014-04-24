using System.Collections.Generic;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class EnqueuedStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new EnqueuedState();
            Assert.Equal(EnqueuedState.StateName, state.Name);
        }

        [Fact]
        public void Ctor_ShouldSetQueue_WhenItWasGiven()
        {
            var state = new EnqueuedState("critical");
            Assert.Equal("critical", state.Queue);
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new EnqueuedState();

            var serializedData = state.SerializeData();

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "EnqueuedAt", "<UtcNow timestamp>" },
                    { "Queue", "default" }
                },
                state.SerializeData());
        }
    }
}
