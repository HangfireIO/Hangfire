using System;
using System.Collections.Generic;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ScheduledStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new ScheduledState(DateTime.UtcNow);
            Assert.Equal(ScheduledState.StateName, state.Name);
        }

        [Fact]
        public void Ctor_SetsTheCorrectDate_WhenTimeSpanIsPassed()
        {
            var state = new ScheduledState(TimeSpan.FromDays(1));
            Assert.True(DateTime.UtcNow.AddDays(1).AddMinutes(-1) < state.EnqueueAt);
            Assert.True(state.EnqueueAt < DateTime.UtcNow.AddDays(1).AddMinutes(1));
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new ScheduledState(DateTime.UtcNow.AddDays(1));

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "ScheduledAt", "<UtcNow timestamp>" },
                    { "EnqueueAt", "<Tomorrow timestamp>" },
                },
                state.Serialize());
        }
    }
}
