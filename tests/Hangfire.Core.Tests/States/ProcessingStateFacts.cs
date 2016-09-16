using System;
using System.Globalization;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ProcessingStateFacts
    {
        private const string WorkerId = "1";
        private const string ServerId = "Server1:4231";

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProcessingState(null, WorkerId));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerNameIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProcessingState(String.Empty, WorkerId));
        }

        [Fact]
        public void StateName_IsCorrect()
        {
            var state = CreateState();
            Assert.Equal(ProcessingState.StateName, state.Name);
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal(JobHelper.SerializeDateTime(state.StartedAt), data["StartedAt"]);
            Assert.Equal(ServerId, data["ServerId"]);
            Assert.Equal(WorkerId.ToString(), data["WorkerId"]);
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

        private ProcessingState CreateState()
        {
            return new ProcessingState(ServerId, WorkerId);
        }
    }
}
