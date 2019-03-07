using System;
using System.Reflection;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class FailedStateFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_IfExceptionParameterIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FailedState(null));
        }

        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new FailedState(new Exception());
            Assert.Equal(FailedState.StateName, state.Name);
        }

        [DataCompatibilityRangeFact(MaxLevel = CompatibilityLevel.Version_110)]
        public void SerializeData_ReturnsCorrectData_Before170()
        {
            var state = new FailedState(new Exception("Message"));

            var serializedData = state.SerializeData();

            Assert.Equal(JobHelper.SerializeDateTime(state.FailedAt), serializedData["FailedAt"]);
            Assert.Equal("System.Exception", serializedData["ExceptionType"]);
            Assert.Equal("Message", serializedData["ExceptionMessage"]);
            Assert.Equal(state.Exception.ToString(), serializedData["ExceptionDetails"]);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeData_ReturnsCorrectData_After170()
        {
            var state = new FailedState(new Exception("Message"));

            var serializedData = state.SerializeData();

            Assert.Equal(3, serializedData.Count);
            Assert.Equal("System.Exception", serializedData["ExceptionType"]);
            Assert.Equal("Message", serializedData["ExceptionMessage"]);
            Assert.Equal(state.Exception.ToString(), serializedData["ExceptionDetails"]);
        }

        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = new FailedState(new Exception());

            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = new FailedState(new Exception());
            Assert.False(state.IgnoreJobLoadException);
        }
    }
}
