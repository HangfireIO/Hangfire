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
            Assert.Equal(SucceededState.Name, state.StateName);
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new SucceededState();
            var data = state.GetProperties(null);

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "SucceededAt", "<UtcNow timestamp>" },
                },
                data);
        }
    }
}
