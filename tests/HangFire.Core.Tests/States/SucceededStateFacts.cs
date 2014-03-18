using System.Collections.Generic;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class SucceededStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new SucceededState();
            Assert.Equal(SucceededState.StateName, state.Name);
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new SucceededState();

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "SucceededAt", "<UtcNow timestamp>" },
                },
                state.Serialize());
        }

        [Fact]
        public void ShouldExpireJobOnApply()
        {
            var state = new SucceededState();
            Assert.True(state.ExpireJobOnApply);
        }
    }
}
