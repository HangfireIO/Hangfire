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
            Assert.True(options.CustomTableNames.Equals(new SqlServerStorageTableConfiguration()));
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsEqualToZero()
        {
            var options = new SqlServerStorageOptions();
            Assert.Throws<ArgumentException>(
                () => options.QueuePollInterval = TimeSpan.Zero);
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

        [Fact]
        public void Set_CustomTableNames_SetsTheValue()
        {
            var options = new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = false
            };
            var randomTableName = Guid.NewGuid().ToString();
            var configuration = new SqlServerStorageTableConfiguration
            {
                JobTableName = randomTableName
            };

            options.CustomTableNames = configuration;
            Assert.True(configuration.Equals(options.CustomTableNames));
        }

        [Fact]
        public void Set_CustomTableNames_ShoudlThrow_WhenSetToNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = false,
                    CustomTableNames = null
                };
            });
        }

        [Fact]
        public void Set_CustomTableNames_ShoudlThrow_WhenSetIncompleteConfiguration()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = false,
                    CustomTableNames = new SqlServerStorageTableConfiguration
                    {
                        HashTableName = null
                    }
                };
            });
        }

        [Fact]
        public void Set_CustomTableNames_ShouldThrow_WhenSetWithPrepareSchemaEnabled()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = true,
                    CustomTableNames = new SqlServerStorageTableConfiguration
                    {
                        HashTableName = null
                    }
                }
            );
        }
    }
}
