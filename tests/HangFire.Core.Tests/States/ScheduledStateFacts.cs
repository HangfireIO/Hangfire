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
            Assert.Equal(ScheduledState.Name, state.StateName);
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new ScheduledState(DateTime.UtcNow.AddDays(1));
            var data = state.GetData(null);

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "ScheduledAt", "<UtcNow timestamp>" },
                    { "EnqueueAt", "<Tomorrow timestamp>" },
                },
                data);
        }
    }
}
