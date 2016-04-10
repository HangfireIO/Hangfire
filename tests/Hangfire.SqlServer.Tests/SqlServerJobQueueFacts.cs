using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerJobQueueFacts
    {
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

        [Fact, CleanDatabase]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsNull()
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                var exception = Assert.Throws<ArgumentNullException>(
                    () => queue.Dequeue(null, CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsEmpty()
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                var exception = Assert.Throws<ArgumentException>(
                    () => queue.Dequeue(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var queue = CreateJobQueue(connection);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource(200);
                var queue = CreateJobQueue(connection);

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@jobId, @queue);
select scope_identity() as Id;";

            // Arrange
            UseConnection(connection =>
            {
                // ReSharper disable once UnusedVariable
                var id = (int)connection.Query(
                    arrangeSql,
                    new { jobId = 1, queue = "default" }).Single().Id;
                var queue = CreateJobQueue(connection);

                // Act
                var payload = (SqlServerFetchedJob)queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal("1", payload.JobId);
                Assert.Equal("default", payload.Queue);
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldDeleteAJob()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnection(connection =>
            {
                connection.Execute(
                    arrangeSql,
                    new { invocationData = "", arguments = "", queue = "default" });
                var queue = CreateJobQueue(connection);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotNull(payload);

                var jobInQueue = connection.Query("select * from HangFire.JobQueue").SingleOrDefault();
                Assert.Null(jobInQueue);
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldFetchATimedOutJobs_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue, FetchedAt)
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
                var queue = CreateJobQueue(connection);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.JobId);
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldSetFetchedAt_OnlyForTheFetchedJob()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
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
                var queue = CreateJobQueue(connection);

                // Act
                var payload = queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                var otherJobFetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId != @id",
                    new { id = payload.JobId }).Single();

                Assert.Null(otherJobFetchedAt);
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                connection.Execute(
                    arrangeSql,
                    new { queue = "critical", invocationData = "", arguments = "" });
                
                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(
                        DefaultQueues,
                        CreateTimingOutCancellationToken()));
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldFetchJobs_FromMultipleQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
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

                var queue = CreateJobQueue(connection);

                var critical = (SqlServerFetchedJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(critical.JobId);
                Assert.Equal("critical", critical.Queue);

                var @default = (SqlServerFetchedJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(@default.JobId);
                Assert.Equal("default", @default.Queue);
            });
        }

        [Fact, CleanDatabase]
        public void Enqueue_AddsAJobToTheQueue()
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                queue.Enqueue(connection, "default", "1");

                var record = connection.Query("select * from HangFire.JobQueue").Single();
                Assert.Equal("1", record.JobId.ToString());
                Assert.Equal("default", record.Queue);
                Assert.Null(record.FetchedAt);
            });
        }

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return source.Token;
        }

        public static void Sample(string arg1, string arg2) { }

        private static SqlServerJobQueue CreateJobQueue(SqlConnection connection)
        {
            var storage = new SqlServerStorage(connection);
            return new SqlServerJobQueue(storage, new SqlServerStorageOptions());
        }

        private static void UseConnection(Action<SqlConnection> action)
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                action(connection);
            }
        }
    }
}
