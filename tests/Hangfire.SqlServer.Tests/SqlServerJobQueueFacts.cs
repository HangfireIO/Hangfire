extern alias ReferencedDapper;

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using ReferencedDapper::Dapper;
using Xunit;
// ReSharper disable ArgumentsStyleLiteral

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerJobQueueFacts
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
        private static readonly string[] DefaultQueues = { "default" };

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerJobQueue(null, new SqlServerStorageOptions()));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerJobQueue(new SqlServerStorage(ConnectionUtils.GetConnectionString()), null));

            Assert.Equal("options", exception.ParamName);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                var exception = Assert.Throws<ArgumentNullException>(
                    () => queue.Dequeue(null, CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsEmpty(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                var exception = Assert.Throws<ArgumentException>(
                    () => queue.Dequeue(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldWaitIndefinitely_WhenThereAreNoJobs(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource(200);
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldFetchAJob_FromTheSpecifiedQueue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (@jobId, @queue);
select scope_identity() as Id;";

            // Arrange
            UseConnection(connection =>
            {
                // ReSharper disable once UnusedVariable
                var id = (int)connection.Query(
                    arrangeSql,
                    new { jobId = 1, queue = "default" }).Single().Id;
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                // Act
                var payload = (SqlServerTransactionJob)queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal("1", payload.JobId);
                Assert.Equal("default", payload.Queue);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_FetchesAJob_WhenJobIdIsLongValue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (@jobId, @queue);
select scope_identity() as Id;";

            // Arrange
            UseConnection(connection =>
            {
                // ReSharper disable once UnusedVariable
                var id = (int)connection.Query(
                    arrangeSql,
                    new { jobId = int.MaxValue + 1L, queue = "default" }).Single().Id;
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                // Act
                var payload = (SqlServerTransactionJob)queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal((int.MaxValue + 1L).ToString(), payload.JobId);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldDeleteAJob(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new { invocationData = "", arguments = "", queue = "default" });
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotNull(payload);

                var jobInQueue = connection.Query($"select * from [{Constants.DefaultSchema}].JobQueue").SingleOrDefault();
                Assert.Null(jobInQueue);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldFetchTimedOutJobs_FromTheSpecifiedQueue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, dateadd(minute, -60, getutcdate()))
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue, FetchedAt)
values (scope_identity(), @queue, @fetchedAt)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new
                    {
                        queue = "default",
                        fetchedAt = DateTime.UtcNow.AddDays(-1),
                        invocationData = "",
                        arguments = ""
                    });
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.JobId);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldSetFetchedAt_OnlyForTheFetchedJob(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "default", invocationData = "", arguments = "" }
                    });
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                var otherJobFetchedAt = connection.Query<DateTime?>(
                    $"select FetchedAt from [{Constants.DefaultSchema}].JobQueue where JobId != @id",
                    new { id = payload.JobId }).Single();

                Assert.Null(otherJobFetchedAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldFetchJobs_OnlyFromSpecifiedQueues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                connection.Execute(
                    arrangeSql,
                    new { queue = "critical", invocationData = "", arguments = "" });
                
                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(
                        DefaultQueues,
                        CreateTimingOutCancellationToken()));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_ShouldFetchJobs_FromMultipleQueues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "critical", invocationData = "", arguments = "" }
                    });

                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

                var critical = (SqlServerTransactionJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(critical.JobId);
                Assert.Equal("critical", critical.Queue);

                var @default = (SqlServerTransactionJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(@default.JobId);
                Assert.Equal("default", @default.Queue);
            }, useMicrosoftDataSqlClient);
        }

        //---
        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldThrowAnException_WhenQueuesCollectionIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                var exception = Assert.Throws<ArgumentNullException>(
                    () => queue.Dequeue(null, CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldThrowAnException_WhenQueuesCollectionIsEmpty(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                var exception = Assert.Throws<ArgumentException>(
                    () => queue.Dequeue(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldWaitIndefinitely_WhenThereAreNoJobs(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource(200);
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldFetchAJob_FromTheSpecifiedQueue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (@jobId, @queue);
select scope_identity() as Id;";

            // Arrange
            UseConnection(connection =>
            {
                var id = (int)connection.Query(
                    arrangeSql,
                    new { jobId = 1, queue = "default" }).Single().Id;
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                // Act
                var payload = (SqlServerTimeoutJob)queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal(id, payload.Id);
                Assert.Equal("1", payload.JobId);
                Assert.Equal("default", payload.Queue);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldLeaveJobInTheQueue_ButSetItsFetchedAtValue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new { invocationData = "", arguments = "", queue = "default" });
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotNull(payload);

                var fetchedAt = connection.Query<DateTime?>(
                    $"select FetchedAt from [{Constants.DefaultSchema}].JobQueue where JobId = @id",
                    new { id = payload.JobId }).Single();

                Assert.NotNull(fetchedAt);
                Assert.True(fetchedAt > DateTime.UtcNow.AddMinutes(-1));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldFetchATimedOutJobs_FromTheSpecifiedQueue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue, FetchedAt)
values (scope_identity(), @queue, @fetchedAt)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new
                    {
                        queue = "default",
                        fetchedAt = DateTime.UtcNow.AddDays(-1),
                        invocationData = "",
                        arguments = ""
                    });
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.JobId);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldSetFetchedAt_OnlyForTheFetchedJob(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "default", invocationData = "", arguments = "" }
                    });
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                var otherJobFetchedAt = connection.Query<DateTime?>(
                    $"select FetchedAt from [{Constants.DefaultSchema}].JobQueue where JobId != @id",
                    new { id = payload.JobId }).Single();

                Assert.Null(otherJobFetchedAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldFetchJobs_OnlyFromSpecifiedQueues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                connection.Execute(
                    arrangeSql,
                    new { queue = "critical", invocationData = "", arguments = "" });

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(
                        DefaultQueues,
                        CreateTimingOutCancellationToken()));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dequeue_InvisibilityTimeout_ShouldFetchJobs_FromMultipleQueues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "critical", invocationData = "", arguments = "" }
                    });

                var queue = CreateJobQueue(connection, invisibilityTimeout: DefaultTimeout);

                var critical = (SqlServerTimeoutJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(critical.JobId);
                Assert.Equal("critical", critical.Queue);

                var @default = (SqlServerTimeoutJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(@default.JobId);
                Assert.Equal("default", @default.Queue);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Enqueue_AddsAJobToTheQueue(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);

#if NETCOREAPP
                using (var transaction = connection.BeginTransaction())
                {
                    queue.Enqueue(connection, transaction, "default", "1");
                    transaction.Commit();
                }
#else
                queue.Enqueue(connection, "default", "1");
#endif

                var record = connection.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();
                Assert.Equal("1", record.JobId.ToString());
                Assert.Equal("default", record.Queue);
                Assert.Null(record.FetchedAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Enqueue_AddsAJob_WhenIdIsLongValue(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection, invisibilityTimeout: null);
                
#if NETCOREAPP
                using (var transaction = connection.BeginTransaction())
                {
                    queue.Enqueue(connection, transaction, "default", (int.MaxValue + 1L).ToString());
                    transaction.Commit();
                }
#else
                queue.Enqueue(connection, "default", (int.MaxValue + 1L).ToString());
#endif

                var record = connection.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();
                Assert.Equal((int.MaxValue + 1L).ToString(), record.JobId.ToString());
            }, useMicrosoftDataSqlClient);
        }

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            return source.Token;
        }

        public static void Sample(string arg1, string arg2) { }

        private static SqlServerJobQueue CreateJobQueue(DbConnection connection, TimeSpan? invisibilityTimeout)
        {
            var storage = new SqlServerStorage(connection);
            return new SqlServerJobQueue(storage, new SqlServerStorageOptions { SlidingInvisibilityTimeout = invisibilityTimeout });
        }

        private static void UseConnection(Action<DbConnection> action, bool useMicrosoftDataSqlClient)
        {
            using (var connection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                action(connection);
            }
        }
    }
}
