using HangFire.Common;
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
        public void SerializeData_ReturnsCorrectData()
        {
            var state = new SucceededState();

            var data = state.SerializeData();

            Assert.Equal(JobHelper.ToStringTimestamp(state.SucceededAt), data["SucceededAt"]);
        }

        [Fact]
        public void IsFinal_ReturnsTrue()
        {
            var state = new SucceededState();
            Assert.True(state.IsFinal);
        }
    }
}
