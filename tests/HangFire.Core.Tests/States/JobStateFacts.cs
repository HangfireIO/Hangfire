using HangFire.Common.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class JobStateFacts
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

            var data = stateMock.Object.GetData(null);

            Assert.NotNull(data);
            Assert.Empty(data);
        }
    }
}
