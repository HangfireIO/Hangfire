using System;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests
{
    public class RecurringJobOptionsFacts
    {
        [Fact]
        public void Ctor_SetTheDefaultValues_ForProperties()
        {
            var options = new RecurringJobOptions();

            Assert.Equal(TimeZoneInfo.Utc, options.TimeZone);
            Assert.Equal("default", options.QueueName);
        }

        [Fact]
        public void SetTimeZone_ThrowsAnException_WhenValueIsNull()
        {
            var options = new RecurringJobOptions();

            Assert.Throws<ArgumentNullException>(() => options.TimeZone = null);
        }

        [Fact]
        public void SetQueueName_ThrowsAnException_WhenValueIsNull()
        {
            var options = new RecurringJobOptions();

            Assert.Throws<ArgumentNullException>(() => options.QueueName = null);
        }
    }
}