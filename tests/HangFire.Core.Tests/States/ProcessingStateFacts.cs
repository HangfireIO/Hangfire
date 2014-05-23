using System;
using System.Globalization;
using HangFire.Common;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ProcessingStateFacts
    {
        private const int WorkerNumber = 1;
        private const string ServerId = "Server1:4231";

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProcessingState(null, WorkerNumber));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerNameIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProcessingState(String.Empty, WorkerNumber));
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

            Assert.Equal(JobHelper.ToStringTimestamp(state.StartedAt), data["StartedAt"]);
            Assert.Equal(ServerId, data["ServerId"]);
            Assert.Equal(WorkerNumber.ToString(CultureInfo.InvariantCulture), data["WorkerNumber"]);
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
            return new ProcessingState(ServerId, WorkerNumber);
        }
    }
}
