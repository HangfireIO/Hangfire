using System;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ManualStateValidationFacts
    {
        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ManualState.ValidateQueueName("queue", string.Empty));
        }

        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ManualState.ValidateQueueName("queue", null));
        }

        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameHasUpperCaseLetters()
        {
            Assert.Throws<ArgumentException>(() => ManualState.ValidateQueueName("queue", "UppercaseLetters"));
        }

        [Fact]
        public void ValidateQueueName_ThrowsAnException_WhenQueueNameHasWhitespaces()
        {
            Assert.Throws<ArgumentException>(() => ManualState.ValidateQueueName("queue", "test test"));
        }

        [Fact]
        public void ValidateQueueName_DoesntThrowAnException_WhenQueueNameHasOnlyLowerCaseLetters()
        {
            // Does not throw
            ManualState.ValidateQueueName("queue", "valid");
        }

        [Fact]
        public void ValidateQueueName_DoesntThrowAnException_WhenQueueNameHasUnderscores()
        {
            // Does not throw
            ManualState.ValidateQueueName("queue", "a_b_c");
        }

        [Fact]
        public void ValidateQueueName_DoesntThrowAnException_WhenValueHasOnlyDigits()
        {
            // Does not throw
            ManualState.ValidateQueueName("queue", "363463");
        }
    }
}
