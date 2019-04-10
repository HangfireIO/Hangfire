using System;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class StorageOptionsFacts
    {
        [Fact]
        public void Ctor_SetsTheDefaultOptions()
        {
            var options = new SqlServerStorageOptions();

            Assert.True(options.QueuePollInterval > TimeSpan.Zero);
#pragma warning disable 618
            Assert.True(options.InvisibilityTimeout > TimeSpan.Zero);
#pragma warning restore 618
            Assert.True(options.JobExpirationCheckInterval > TimeSpan.Zero);
            Assert.True(options.PrepareSchemaIfNecessary);
        }

        [Fact]
        public void Set_QueuePollInterval_DoesNotThrow_WhenGivenIntervalIsEqualToZero()
        {
            var options = new SqlServerStorageOptions();
            options.QueuePollInterval = TimeSpan.Zero;
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsNegative()
        {
            var options = new SqlServerStorageOptions();
            Assert.Throws<ArgumentException>(
                () => options.QueuePollInterval = TimeSpan.FromSeconds(-1));
        }

        [Fact]
        public void Set_QueuePollInterval_SetsTheValue()
        {
            var options = new SqlServerStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(1) };
            Assert.Equal(TimeSpan.FromSeconds(1), options.QueuePollInterval);
        }
    }
}
