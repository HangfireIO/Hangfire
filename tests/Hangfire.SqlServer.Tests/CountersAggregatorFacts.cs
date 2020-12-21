extern alias ReferencedDapper;

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using ReferencedDapper::Dapper;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class CountersAggregatorFacts
    {
        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void CountersAggregatorExecutesProperly(bool useMicrosoftDataSqlClient)
        {
            var createSql = $@"
insert into [{Constants.DefaultSchema}].Counter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";

            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddHours(1) });

                var aggregator = CreateAggregator(connection);
                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act
                aggregator.Execute(cts.Token);

                // Assert
                Assert.Equal(1, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].AggregatedCounter").Single());
            }
        }

        private static DbConnection CreateConnection(bool useMicrosoftDataSqlClient)
        {
            return ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient);
        }

        private static CountersAggregator CreateAggregator(DbConnection connection)
        {
            var storage = new SqlServerStorage(connection);
            return new CountersAggregator(storage, TimeSpan.Zero);
        }
    }
}
