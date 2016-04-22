using System;
using Hangfire.Validation;
using Xunit;

namespace Hangfire.Core.Tests.Validation
{
    public class QueueValidatorFacts
    {
        [Fact]
        public void ValidateName_ThrowsAnException_WhenQueueNameIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => QueueValidator.ValidateName(string.Empty));
        }

        [Fact]
        public void ValidateName_ThrowsAnException_WhenQueueNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => QueueValidator.ValidateName(null));
        }

        [Fact]
        public void ValidateName_ThrowsAnException_WhenQueueNameHasUpperCaseLetters()
        {
            Assert.Throws<ArgumentException>(() => QueueValidator.ValidateName("UppercaseLetters"));
        }

        [Fact]
        public void ValidateName_ThrowsAnException_WhenQueueNameHasWhitespaces()
        {
            Assert.Throws<ArgumentException>(() => QueueValidator.ValidateName("test test"));
        }

        [Fact]
        public void ValidateName_DoesntThrowAnException_WhenQueueNameHasOnlyLowerCaseLetters()
        {
            Assert.DoesNotThrow(() => QueueValidator.ValidateName("valid"));
        }

        [Fact]
        public void ValidateName_DoesntThrowAnException_WhenQueueNameHasUnderscores()
        {
            Assert.DoesNotThrow(() => QueueValidator.ValidateName("a_b_c"));
        }

        [Fact]
        public void ValidateName_DoesntThrowAnException_WhenValueHasOnlyDigits()
        {
            Assert.DoesNotThrow(() => QueueValidator.ValidateName("363463"));
        }
    }
}
