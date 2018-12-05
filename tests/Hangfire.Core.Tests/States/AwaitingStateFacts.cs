using System;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class AwaitingStateFacts
    {
        [Fact]
        public void StateName_IsEqualToAwaiting()
        {
            Assert.Equal("Awaiting", AwaitingState.StateName);
        }

        [Fact]
        public void NameProperty_ReturnsStateName()
        {
            var state = CreateState();
            Assert.Equal(AwaitingState.StateName, state.Name);
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = CreateState();
            
            var data = state.SerializeData();

            Assert.Equal(state.ParentId, data["ParentId"]);
            Assert.Matches(
                "^{\"\\$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\"," +
                "\"EnqueuedAt\":\"\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{6,7}(Z|[+-]\\d{2}:\\d{2})\"}$", 
                data["NextState"]);
            Assert.Equal(state.Options.ToString("D"), data["Options"]);
            Assert.Equal(state.Expiration.ToString(), data["Expiration"]);
        }
        
        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = CreateState();
            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = CreateState();
            Assert.False(state.IgnoreJobLoadException);
        }

        private static AwaitingState CreateState()
        {
            return new AwaitingState("1", new EnqueuedState(), JobContinuationOptions.OnlyOnSucceededState, TimeSpan.FromDays(1));
        }
    }
}
