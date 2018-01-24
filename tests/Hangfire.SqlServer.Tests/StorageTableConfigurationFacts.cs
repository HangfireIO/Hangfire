using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class StorageTableConfigurationFacts
    {
        [Fact]
        public void Ctor_SetsDefaultTableNames()
        {
            var configuration = new SqlServerStorageTableConfiguration();

            Assert.True(configuration["Schema"] == Constants.DefaultNameSchemaTable);
            Assert.True(configuration["Job"] == Constants.DefaultNameJobTable);
            Assert.True(configuration["State"] == Constants.DefaultNameStateTable);
            Assert.True(configuration["JobParameter"] == Constants.DefaultNameJobParameterTable);
            Assert.True(configuration["JobQueue"] == Constants.DefaultNameJobQueueTable);
            Assert.True(configuration["Server"] == Constants.DefaultNameServerTable);
            Assert.True(configuration["Hash"] == Constants.DefaultNameHashTable);
            Assert.True(configuration["List"] == Constants.DefaultNameListTable);
            Assert.True(configuration["Set"] == Constants.DefaultNameSetTable);
            Assert.True(configuration["Value"] == Constants.DefaultNameValueTable);
            Assert.True(configuration["Counter"] == Constants.DefaultNameCounterTable);
            Assert.True(configuration["AggregatedCounter"] == Constants.DefaultNameAggregatedCounterTable);
        }

        [Theory]
        [MemberData(nameof(GetAllConfigurableTableNames))]
        public void Set_EveryConfigurableTable_SetsValue(string configurableTableName)
        {
            var configuration = new SqlServerStorageTableConfiguration();
            var randomTableName = Guid.NewGuid().ToString();

            configuration[configurableTableName] = randomTableName;

            Assert.True(configuration[configurableTableName] == randomTableName);
        }

        [Theory]
        [MemberData(nameof(GetAllConfigurableTableNames))]
        public void Set_EveryConfigurableTable_ShouldThrow_IfSetNull
            (string configurableTableName)
        {
            var configuration = new SqlServerStorageTableConfiguration();

            Assert.Throws<ArgumentNullException>(() =>
            {
                configuration[configurableTableName] = null;
            });
        }

        [Theory]
        [MemberData(nameof(GetAllConfigurableTableNames))]
        public void Set_EveryConfigurableTable_IfSetEmptyString_MakesConfigurationIncomplete
            (string configurableTableName)
        {
            var configuration = new SqlServerStorageTableConfiguration();

            Assert.Throws<ArgumentNullException>(() =>
            {
                configuration[configurableTableName] = string.Empty;
            });
        }

        public static IEnumerable<object[]> GetAllConfigurableTableNames()
        {
            return new SqlServerStorageTableConfiguration().AvailableConfigurableTableNames.Select(tableName => new[] { tableName });
        }
    }
}
