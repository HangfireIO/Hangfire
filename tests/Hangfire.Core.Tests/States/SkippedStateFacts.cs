using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class SkippedStateFacts
    {
        [Fact]
        public void StateName_IsEqualToSkipped()
        {
            Assert.Equal("Skipped", SkippedState.StateName);
        }

        [Fact]
        public void NameProperty_ReturnsStateName()
        {
            var state = CreateState();
            Assert.Equal(SkippedState.StateName, state.Name);
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal(JobHelper.SerializeDateTime(state.SkippedAt), data["SkippedAt"]);
       
        }

        [Fact]
        public void SerializeData_DoesNotAppendEntry_ForNullResult()
        {
            var state = new SkippedState();

            var data = state.SerializeData();

            Assert.False(data.ContainsKey("Result"));
        }


        [Fact]
        public void IsFinal_ReturnsTrue()
        {
            var state = CreateState();
            Assert.True(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = CreateState();
            Assert.False(state.IgnoreJobLoadException);
        }

        private static SkippedState CreateState()
        {
            return new SkippedState();
        }
    }
}
