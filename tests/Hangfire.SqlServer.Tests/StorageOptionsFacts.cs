using System;
using Hangfire.Sql;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class StorageOptionsFacts
    {
        [Fact]
        public void Ctor_SetsTheDefaultOptions()
        {
            var options = new SqlStorageOptions();

            Assert.True(options.QueuePollInterval > TimeSpan.Zero);
            Assert.True(options.InvisibilityTimeout > TimeSpan.Zero);
            Assert.True(options.PrepareSchemaIfNecessary);
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsEqualToZero()
        {
            var options = new SqlStorageOptions();
            Assert.Throws<ArgumentException>(
                () => options.QueuePollInterval = TimeSpan.Zero);
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsNegative()
        {
            var options = new SqlStorageOptions();
            Assert.Throws<ArgumentException>(
                () => options.QueuePollInterval = TimeSpan.FromSeconds(-1));
        }

        [Fact]
        public void Set_QueuePollInterval_SetsTheValue()
        {
            var options = new SqlStorageOptions();
            options.QueuePollInterval = TimeSpan.FromSeconds(1);
            Assert.Equal(TimeSpan.FromSeconds(1), options.QueuePollInterval);
        }
    }
}
