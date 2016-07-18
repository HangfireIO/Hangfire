using System;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class EnqueuedStateValidationFacts
    {
        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => EnqueuedState.ValidateQueueName("queue", string.Empty));
        }

        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => EnqueuedState.ValidateQueueName("queue", null));
        }

        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameHasUpperCaseLetters()
        {
            Assert.Throws<ArgumentException>(() => EnqueuedState.ValidateQueueName("queue", "UppercaseLetters"));
        }

        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameHasWhitespaces()
        {
            Assert.Throws<ArgumentException>(() => EnqueuedState.ValidateQueueName("queue", "test test"));
        }

        [Fact]
        public void ValidateQueueName_DoesntThrowAnException_WhenQueueNameHasOnlyLowerCaseLetters()
        {
            // Does not throw
            EnqueuedState.ValidateQueueName("queue", "valid");
        }

        [Fact]
        public void ValidateQueueName_DoesntThrowAnException_WhenQueueNameHasUnderscores()
        {
            // Does not throw
            EnqueuedState.ValidateQueueName("queue", "a_b_c");
        }

        [Fact]
        public void ValidateQueueName_DoesntThrowAnException_WhenValueHasOnlyDigits()
        {
            // Does not throw
            EnqueuedState.ValidateQueueName("queue", "363463");
        }
    }
}
