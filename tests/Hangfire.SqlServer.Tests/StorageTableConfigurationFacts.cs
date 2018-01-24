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

            Assert.True(configuration.SchemaTableName == Constants.DefaultNameSchemaTable);
            Assert.True(configuration.JobTableName == Constants.DefaultNameJobTable);
            Assert.True(configuration.StateTableName == Constants.DefaultNameStateTable);
            Assert.True(configuration.JobParameterTableName == Constants.DefaultNameJobParameterTable);
            Assert.True(configuration.JobQueueTableName == Constants.DefaultNameJobQueueTable);
            Assert.True(configuration.ServerTableName == Constants.DefaultNameServerTable);
            Assert.True(configuration.HashTableName == Constants.DefaultNameHashTable);
            Assert.True(configuration.ListTableName == Constants.DefaultNameListTable);
            Assert.True(configuration.SetTableName == Constants.DefaultNameSetTable);
            Assert.True(configuration.ValueTableName == Constants.DefaultNameValueTable);
            Assert.True(configuration.CounterTableName == Constants.DefaultNameCounterTable);
            Assert.True(configuration.AggregatedCounterTableName == Constants.DefaultNameAggregatedCounterTable);
        }

        [Fact]
        public void Get_IsCompleteConfiguration_ReturnsTrue_ForAllFilled()
        {
            var configuration = new SqlServerStorageTableConfiguration();

            Assert.True(configuration.IsCompleteConfiguration);
        }

        [Theory]
        [MemberData(nameof(GetAllConfigurableTableNames))]
        public void Set_EveryConfigurableTable_SetsValue(PropertyInfo configurableTableName)
        {
            var configuration = new SqlServerStorageTableConfiguration();
            var randomTableName = Guid.NewGuid().ToString();

            configurableTableName.SetValue(configuration, randomTableName);

            Assert.True((string)configurableTableName.GetValue(configuration) == randomTableName);
        }

        [Theory]
        [MemberData(nameof(GetAllConfigurableTableNames))]
        public void Set_EveryConfigurableTable_IfSetNull_MakesConfigurationIncomplete
            (PropertyInfo configurableTableName)
        {
            var configuration = new SqlServerStorageTableConfiguration();

            configurableTableName.SetValue(configuration, null);

            Assert.True(!configuration.IsCompleteConfiguration);
        }

        [Theory]
        [MemberData(nameof(GetAllConfigurableTableNames))]
        public void Set_EveryConfigurableTable_IfSetEmptyString_MakesConfigurationIncomplete
            (PropertyInfo configurableTableName)
        {
            var configuration = new SqlServerStorageTableConfiguration();

            configurableTableName.SetValue(configuration, string.Empty);

            Assert.True(!configuration.IsCompleteConfiguration);
        }

        private static IEnumerable<object[]> GetAllConfigurableTableNames()
        {
            return typeof(SqlServerStorageTableConfiguration)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetSetMethod() != null)
                .Select(prop => new[] { prop });
        }
    }
}
