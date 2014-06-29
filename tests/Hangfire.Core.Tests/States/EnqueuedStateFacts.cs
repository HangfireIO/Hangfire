using System;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class EnqueuedStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new EnqueuedState();
            Assert.Equal(EnqueuedState.StateName, state.Name);
        }

        [Fact]
        public void Ctor_ShouldSetQueue_WhenItWasGiven()
        {
            var state = new EnqueuedState("critical");
            Assert.Equal("critical", state.Queue);
        }

        [Fact]
        public void SetQueue_ThrowsAnException_WhenQueueValueIsEmpty()
        {
            var state = new EnqueuedState();
            Assert.Throws<ArgumentNullException>(() => state.Queue = String.Empty);
        }

        [Fact]
        public void SetQueue_ThrowsAnException_WhenValueIsNotInAGivenFormat()
        {
            var state = new EnqueuedState();

            Assert.Throws<ArgumentException>(() => state.Queue = "UppercaseLetters");
            Assert.Throws<ArgumentException>(() => state.Queue = "punctuation:un-allowed");
            Assert.Throws<ArgumentException>(() => state.Queue = "моя_твоя_непонимать");
        }

        [Fact]
        public void SetQueue_DoesNotThrowException_WhenValueIsInACorrectFormat()
        {
            var state = new EnqueuedState();

            Assert.DoesNotThrow(() => state.Queue = "lowercasedcharacters");
            Assert.DoesNotThrow(() => state.Queue = "underscores_allowed");
            Assert.DoesNotThrow(() => state.Queue = "1234567890_allowed");
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = new EnqueuedState();

            var serializedData = state.SerializeData();

            Assert.Equal(state.Queue, serializedData["Queue"]);
            Assert.Equal(JobHelper.SerializeDateTime(state.EnqueuedAt), serializedData["EnqueuedAt"]);
        }

        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = new EnqueuedState();

            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = new EnqueuedState();

            Assert.False(state.IgnoreJobLoadException);
        }
    }
}
