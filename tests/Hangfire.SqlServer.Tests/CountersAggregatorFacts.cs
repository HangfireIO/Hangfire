﻿using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class CountersAggregatorFacts
    {
        [Fact, CleanDatabase]
        public void CountersAggregatorExecutesProperly()
        {
            const string createSql = @"
insert into HangFire.Counter ([Key], [Value], ExpireAt) 
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
                Assert.Equal(1, connection.Query<int>(@"select count(*) from HangFire.AggregatedCounter").Single());
            }
        }

        [Fact, CleanDatabase]
        public void CountersAggregator_HandlesAggregatorIdCanExceedInt32Max()
        {
            const string createSql = @"
insert into HangFire.Counter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";

            using (var connection = CreateConnection())
            {
                // Arrange
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddHours(1) });

                var aggregator = CreateAggregator(connection);
                var cts = new CancellationTokenSource();
                cts.Cancel();

                connection.Query($"DBCC CHECKIDENT('HangFire.AggregatedCounter', RESEED, {int.MaxValue + 1L});");

                // Act
                aggregator.Execute(cts.Token);

                // Assert
                Assert.True(int.MaxValue < connection.Query<long>(@"select Id from HangFire.AggregatedCounter").Single());
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
