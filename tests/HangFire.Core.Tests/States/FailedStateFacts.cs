using System;
using System.Collections.Generic;
using FluentAssertions;
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
        public void Ctor_SetsAllProperties()
        {
            var exception = new Exception();
            var state = new FailedState(exception);

            state.Exception.Should().BeSameAs(exception);
        }

        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new FailedState(new Exception());

            state.StateName.Should().Be(FailedState.Name);
        }

        [Fact]
        public void GetProperties_ReturnsCorrectProperties()
        {
            var state = new FailedState(new Exception("Message"));
            var properties = state.GetProperties(null);

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "FailedAt", "<UtcNow timestamp>" },
                    { "ExceptionType", "System.Exception" },
                    { "ExceptionMessage", "Message" },
                    { "ExceptionDetails", "<Non-empty>" }
                }, 
                properties);
        }
    }
}
