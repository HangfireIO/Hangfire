using System;
using HangFire.Common;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ProcessingStateFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenServerNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProcessingState(null));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerNameIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProcessingState(String.Empty));
        }

        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new ProcessingState("Server1");
            Assert.Equal(ProcessingState.StateName, state.Name);
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = new ProcessingState("Server1");

            var data = state.SerializeData();

            Assert.Equal(JobHelper.ToStringTimestamp(state.StartedAt), data["StartedAt"]);
            Assert.Equal("Server1", state.ServerName);
        }
    }
}
