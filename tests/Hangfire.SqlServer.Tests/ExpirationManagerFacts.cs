extern alias ReferencedDapper;

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using ReferencedDapper::Dapper;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class ExpirationManagerFacts
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null, TimeSpan.Zero));
        }

        [Fact, CleanDatabase]
        public void Execute_RemovesOutdatedRecords()
        {
            using (var connection = CreateConnection())
            {
                CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(-1));
                var manager = CreateManager(connection);

                manager.Execute(_cts.Token);

                Assert.True(IsEntryExpired(connection));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            using (var connection = CreateConnection())
            {
                CreateExpirationEntry(connection, null);
                var manager = CreateManager(connection);

                manager.Execute(_cts.Token);

                Assert.False(IsEntryExpired(connection));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            using (var connection = CreateConnection())
            {
                CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(1));
                var manager = CreateManager(connection);

                manager.Execute(_cts.Token);

                Assert.False(IsEntryExpired(connection));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_AggregatedCounterTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].AggregatedCounter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Counter").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_JobTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt, ExpireAt) 
values ('', '', getutcdate(), @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Job").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_ListTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].List ([Key], ExpireAt) 
values ('key', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_SetTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Score], [Value], ExpireAt) 
values ('key', 0, '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_HashTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field], [Value], ExpireAt) 
values ('key', 'field', '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Hash").Single());
            }
        }

        private static void CreateExpirationEntry(SqlConnection connection, DateTime? expireAt)
        {
            var insertSql = $@"
insert into [{Constants.DefaultSchema}].AggregatedCounter ([Key], [Value], [ExpireAt])
values (N'key', 1, @expireAt)";

            connection.Execute(insertSql, new { expireAt });
        }

        private static bool IsEntryExpired(SqlConnection connection)
        {
            var count = connection.Query<int>(
                    $"select count(*) from [{Constants.DefaultSchema}].AggregatedCounter where [Key] = N'key'").Single();
            return count == 0;
        }

        private SqlConnection CreateConnection()
        {
            return ConnectionUtils.CreateConnection();
        }

        private ExpirationManager CreateManager(SqlConnection connection)
        {
            var storage = new SqlServerStorage(connection);
            return new ExpirationManager(storage, TimeSpan.Zero);
        }
    }
}
