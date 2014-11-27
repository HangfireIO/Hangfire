using System;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Sql;
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
        public void Execute_RemovesOutdatedRecords() {
            int entryId = CreateExpirationEntry(DateTime.UtcNow.AddMonths(-1));
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.True(IsEntryExpired(entryId));
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            var entryId = CreateExpirationEntry(null);
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.False(IsEntryExpired(entryId));
        }

        [Fact, CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            var entryId = CreateExpirationEntry(DateTime.Now.AddMonths(1));
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.False(IsEntryExpired(entryId));
        }

        [Fact, CleanDatabase("HangFire.Counter")]
        public void Execute_Processes_CounterTable() {
            ConnectionUtils.UseConnection((connection, tx) => {
                // Arrange
                const string createSql = @"
insert into HangFire.Counter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) }, tx);
            });
                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
            ConnectionUtils.UseConnection((connection, tx) => 
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Counter", transaction:tx).Single()));
        }

        [Fact, CleanDatabase("HangFire.Job")]
        public void Execute_Processes_JobTable()
        {
            // Arrange
                const string createSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt) 
values ('', '', getutcdate(), @expireAt)";
            ConnectionUtils.UseConnection((connection, tx) => 
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) }, tx));
            var manager = CreateManager();

            // Act
            manager.Execute(_token);

                // Assert
            ConnectionUtils.UseConnection((connection, tx) => {
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Job", transaction:tx).Single());
            });
        }

        [Fact, CleanDatabase("HangFire.List")]
        public void Execute_Processes_ListTable()
        {
            // Arrange
            const string createSql = @"
insert into HangFire.List ([Key], ExpireAt) 
values ('key', @expireAt)";
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) }, transaction:tx);
            });
                var manager = CreateManager();

                // Act
                manager.Execute(_token);

                // Assert
            ConnectionUtils.UseConnection((connection, tx) => {
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.List", transaction: tx).Single());
            });
        }

        [Fact, CleanDatabase("HangFire.[Set]")]
        public void Execute_Processes_SetTable()
        {
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                // Arrange
                const string createSql = @"
insert into HangFire.[Set] ([Key], [Score], [Value], ExpireAt) 
values ('key', 0, '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) }, tx);
            });
            var manager = CreateManager();

            // Act
            manager.Execute(_token);

                // Assert
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.[Set]", transaction: tx).Single());
            });
        }

        [Fact, CleanDatabase("HangFire.Hash")]
        public void Execute_Processes_HashTable()
        {
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                // Arrange
                const string createSql = @"
insert into HangFire.Hash ([Key], [Field], [Value], ExpireAt) 
values ('key', 'field', '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) }, tx);
            });
                var manager = CreateManager();

                // Act
                manager.Execute(_token);
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                // Assert
                Assert.Equal(0, connection.Query<int>(@"select count(*) from HangFire.Hash", transaction:tx).Single());
            });
        }

        private static int CreateExpirationEntry(DateTime? expireAt)
        {
            const string insertSql = @"
insert into HangFire.Counter ([Key], [Value], [ExpireAt])
values ('key', 1, @expireAt)
select scope_identity() as Id";
            int recordId = 0;
            ConnectionUtils.UseConnection((connection, tx) => {
                var id = connection.Query(insertSql, new { @expireAt = expireAt }, transaction: tx).Single();
                recordId = (int)id.Id;
            });
            return recordId;
        }

        private static bool IsEntryExpired(int entryId)
        {
            int count = -1;
            ConnectionUtils.UseConnection((connection, tx) => {
                count = connection.Query<int>(
                    "select count(*) from HangFire.Counter where Id = @id", new { id = entryId }, tx).Single();
            });
            return count == 0;
        }

        private ExpirationManager CreateManager()
        {
            return new ExpirationManager(_storage);
        }
    }
}
