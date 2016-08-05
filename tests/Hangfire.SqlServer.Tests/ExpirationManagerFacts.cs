using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;
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
                var entryId = CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(-1));
                var manager = CreateManager(connection);

                manager.Execute(_cts.Token);

                Assert.True(IsEntryExpired(connection, entryId));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            using (var connection = CreateConnection())
            {
                var entryId = CreateExpirationEntry(connection, null);
                var manager = CreateManager(connection);

                manager.Execute(_cts.Token);

                Assert.False(IsEntryExpired(connection, entryId));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            using (var connection = CreateConnection())
            {
                var entryId = CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(1));
                var manager = CreateManager(connection);

                manager.Execute(_cts.Token);

                Assert.False(IsEntryExpired(connection, entryId));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_AggregatedCounterTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.AggregatedCounter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Counter").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_JobTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt) 
values ('', '', getutcdate(), @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);
                _cts.CancelAfter(TimeSpan.FromSeconds(10));
                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Job").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_ListTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.List ([Key], ExpireAt) 
values ('key', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.List").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_SetTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.[Set] ([Key], [Score], [Value], ExpireAt) 
values ('key', 0, '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.[Set]").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_HashTable()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.Hash ([Key], [Field], [Value], ExpireAt) 
values ('key', 'field', '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(connection);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Hash").Single());
            }
        }

        private static int CreateExpirationEntry(SqlConnection connection, DateTime? expireAt)
        {
            const string insertSql = @"
insert into HangFire.AggregatedCounter ([Key], [Value], [ExpireAt])
values ('key', 1, @expireAt)
select scope_identity() as Id";

            var id = connection.Query(insertSql, new { expireAt }).Single();
            var recordId = (int) id.Id;
            return recordId;
        }

        private static bool IsEntryExpired(SqlConnection connection, int entryId)
        {
            var count = connection.Query<int>(
                    "select count(*) from HangFire.AggregatedCounter where Id = @id", new { id = entryId }).Single();
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
