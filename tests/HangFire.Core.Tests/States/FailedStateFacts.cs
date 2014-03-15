using System;
using System.Collections.Generic;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests.States
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
            Assert.Equal(FailedState.Name, state.StateName);
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new FailedState(new Exception("Message"));
            var data = state.GetProperties(null);

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "FailedAt", "<UtcNow timestamp>" },
                    { "ExceptionType", "System.Exception" },
                    { "ExceptionMessage", "Message" },
                    { "ExceptionDetails", "<Non-empty>" }
                }, 
                data);
        }
    }
}
