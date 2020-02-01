extern alias ReferencedDapper;

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using ReferencedDapper::Dapper;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class CountersAggregatorFacts
    {
        [Fact, CleanDatabase]
        public void CountersAggregatorExecutesProperly()
        {
            var createSql = $@"
insert into [{Constants.DefaultSchema}].Counter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";

            using (var connection = CreateConnection())
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

        private static SqlConnection CreateConnection()
        {
            return ConnectionUtils.CreateConnection();
        }

        private static CountersAggregator CreateAggregator(SqlConnection connection)
        {
            var storage = new SqlServerStorage(connection);
            return new CountersAggregator(storage, TimeSpan.Zero);
        }
    }
}
