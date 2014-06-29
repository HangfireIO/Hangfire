using System;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class ExpirationManagerFacts
    {
        private readonly SqlServerStorage _storage;
        private readonly CancellationToken _token;

        public ExpirationManagerFacts()
        {
            _storage = new SqlServerStorage(ConnectionUtils.GetConnectionString());
            _token = new CancellationToken(true);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null));
        }

        [Fact, CleanDatabase]
        public void Execute_RemovesOutdatedRecords()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var entryId = CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(-1));
                var manager = CreateManager();

                manager.Execute(_token);

                Assert.True(IsEntryExpired(connection, entryId));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var entryId = CreateExpirationEntry(connection, null);
                var manager = CreateManager();

                manager.Execute(_token);

                Assert.False(IsEntryExpired(connection, entryId));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var entryId = CreateExpirationEntry(connection, DateTime.Now.AddMonths(1));
                var manager = CreateManager();

                manager.Execute(_token);

                Assert.False(IsEntryExpired(connection, entryId));
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_CounterTable()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.Counter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Counter").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_JobTable()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt) 
values ('', '', getutcdate(), @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Job").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_ListTable()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.List ([Key], ExpireAt) 
values ('key', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.List").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_SetTable()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.[Set] ([Key], [Score], [Value], ExpireAt) 
values ('key', 0, '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.[Set]").Single());
            }
        }

        [Fact, CleanDatabase]
        public void Execute_Processes_HashTable()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                // Arrange
                const string createSql = @"
insert into HangFire.Hash ([Key], [Field], [Value], ExpireAt) 
values ('key', 'field', '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Hash").Single());
            }
        }

        private static int CreateExpirationEntry(IDbConnection connection, DateTime? expireAt)
        {
            const string insertSql = @"
insert into HangFire.Counter ([Key], [Value], [ExpireAt])
values ('key', 1, @expireAt)
select scope_identity() as Id";

            var id = connection.Query(insertSql, new { @expireAt = expireAt }).Single();
            var recordId = (int) id.Id;
            return recordId;
        }

        private static bool IsEntryExpired(IDbConnection connection, int entryId)
        {
            var count = connection.Query<int>(
                    "select count(*) from HangFire.Counter where Id = @id", new { id = entryId }).Single();
            return count == 0;
        }

        private ExpirationManager CreateManager()
        {
            return new ExpirationManager(_storage);
        }
    }
}
