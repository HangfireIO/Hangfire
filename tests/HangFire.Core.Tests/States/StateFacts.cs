using HangFire.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateFacts
    {
        [Fact]
        public void ShouldNotExpireJobOnApplyByDefault()
        {
            var stateMock = new Mock<State>();
            stateMock.CallBase = true;

            Assert.False(stateMock.Object.ExpireJobOnApply);
        }

        [Fact]
        public void ShouldReturnEmptyStateDataByDefault()
        {
            var stateMock = new Mock<State>();
            stateMock.CallBase = true;

            Assert.Empty(stateMock.Object.SerializeData());
        }
    }
}
