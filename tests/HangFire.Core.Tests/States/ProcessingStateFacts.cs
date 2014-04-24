using System;
using System.Collections.Generic;
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
        public void GetStateData_ReturnsCorrectData()
        {
            var state = new ProcessingState("Server1");

            DictionaryAssert.ContainsFollowingItems(
                new Dictionary<string, string>
                {
                    { "StartedAt", "<UtcNow timestamp>" },
                    { "ServerName", "Server1" },
                },
                state.SerializeData());
        }
    }
}
