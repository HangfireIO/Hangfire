extern alias ReferencedDapper;

using System;
using System.Data.Common;
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
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null, TimeSpan.Zero, TimeSpan.FromTicks(1)));
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_RemovesOutdatedRecords(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(-1));
                var manager = CreateManager(useMicrosoftDataSqlClient);

                manager.Execute(_cts.Token);

                Assert.True(IsEntryExpired(connection));
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                CreateExpirationEntry(connection, null);
                var manager = CreateManager(useMicrosoftDataSqlClient);

                manager.Execute(_cts.Token);

                Assert.False(IsEntryExpired(connection));
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                CreateExpirationEntry(connection, DateTime.UtcNow.AddMonths(1));
                var manager = CreateManager(useMicrosoftDataSqlClient);

                manager.Execute(_cts.Token);

                Assert.False(IsEntryExpired(connection));
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_Processes_AggregatedCounterTable(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].AggregatedCounter ([Key], [Value], ExpireAt) 
values ('key', 1, @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(useMicrosoftDataSqlClient);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Counter").Single());
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_Processes_JobTable(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt, ExpireAt) 
values ('', '', getutcdate(), @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(useMicrosoftDataSqlClient);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Job").Single());
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_Processes_StateTable_WhenOptionIsConfigured(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                var now = DateTime.UtcNow;
                var createSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, StateName, CreatedAt)
values ('', '', '', getutcdate());
declare @JobId bigint;
set @JobId = scope_identity();
insert into [{Constants.DefaultSchema}].State (JobId, Name, CreatedAt)
values (@JobId, 'old-state-1', @createdAt1);
insert into [{Constants.DefaultSchema}].State (JobId, Name, CreatedAt)
values (@JobId, 'old-state-2', @createdAt2);
insert into [{Constants.DefaultSchema}].State (JobId, Name, CreatedAt)
values (@JobId, 'current-state', @createdAt3);
declare @StateId bigint;
set @StateId = scope_identity();
update [{Constants.DefaultSchema}].Job set StateId = @StateId;
select @JobId as Id;";

                var jobId = connection
                    .Query(createSql, new { createdAt1 = now.AddDays(-1), createdAt2 = now.AddMonths(-1), createdAt3 = now.AddMonths(-1) })
                    .Single().Id;

                var manager = CreateManager(useMicrosoftDataSqlClient, TimeSpan.FromDays(7));

                // Act
                manager.Execute(_cts.Token);

                // Assert
                var states = connection
                    .Query<string>($"select [Name] from [{Constants.DefaultSchema}].State where JobId = @jobId order by Id", new { jobId })
                    .ToList();
                
                Assert.Equal("old-state-1", states[0]);
                Assert.Equal("current-state", states[1]);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_Processes_ListTable(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].List ([Key], ExpireAt) 
values ('key', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(useMicrosoftDataSqlClient);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single());
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_Processes_SetTable(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Score], [Value], ExpireAt) 
values ('key', 0, '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(useMicrosoftDataSqlClient);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single());
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Execute_Processes_HashTable(bool useMicrosoftDataSqlClient)
        {
            using (var connection = CreateConnection(useMicrosoftDataSqlClient))
            {
                // Arrange
                var createSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field], [Value], ExpireAt) 
values ('key', 'field', '', @expireAt)";
                connection.Execute(createSql, new { expireAt = DateTime.UtcNow.AddMonths(-1) });

                var manager = CreateManager(useMicrosoftDataSqlClient);

                // Act
                manager.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, connection.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Hash").Single());
            }
        }

        private static void CreateExpirationEntry(DbConnection connection, DateTime? expireAt)
        {
            var insertSql = $@"
insert into [{Constants.DefaultSchema}].AggregatedCounter ([Key], [Value], [ExpireAt])
values (N'key', 1, @expireAt)";

            connection.Execute(insertSql, new { expireAt });
        }

        private static bool IsEntryExpired(DbConnection connection)
        {
            var count = connection.Query<int>(
                    $"select count(*) from [{Constants.DefaultSchema}].AggregatedCounter where [Key] = N'key'").Single();
            return count == 0;
        }

        private DbConnection CreateConnection(bool useMicrosoftDataSqlClient)
        {
            return ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient);
        }

        private ExpirationManager CreateManager(bool useMicrosoftDataSqlClient, TimeSpan? stateExpirationTimeout = null)
        {
            var storage = new SqlServerStorage(() => ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient));
            return new ExpirationManager(storage, stateExpirationTimeout ?? TimeSpan.Zero, TimeSpan.FromTicks(1));
        }
    }
}
