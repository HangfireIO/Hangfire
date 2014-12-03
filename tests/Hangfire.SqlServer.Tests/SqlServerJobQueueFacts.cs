using System;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Sql;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerJobQueueFacts
    {
        private static readonly string[] DefaultQueues = { "default" };

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlJobQueue(null, new SqlBook(), new SqlStorageOptions()));

            Assert.Equal("connectionProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlJobQueue(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsNull() {
            SqlJobQueue queue = null;
            ConnectionUtils.UseConnection((connection, tx) => {
                queue = CreateJobQueue();
            });
            var exception = Assert.Throws<ArgumentNullException>(
                () => queue.Dequeue(null, CreateTimingOutCancellationToken()));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsEmpty()
        {
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                var queue = CreateJobQueue();

                var exception = Assert.Throws<ArgumentException>(
                    () => queue.Dequeue(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var queue = CreateJobQueue();

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact, CleanDatabase]
        public void Dequeue_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                var cts = new CancellationTokenSource(200);
                var queue = CreateJobQueue();

                Assert.Throws<OperationCanceledException>(
                    () => queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact, CleanDatabase("HangFire.JobQueue")]
        public void Dequeue_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@jobId, @queue);
select scope_identity() as Id;";

            // Arrange
            int id = 0;
            SqlJobQueue queue = null;
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                id = (int)connection.Query(
                    arrangeSql,
                    new { jobId = 1, queue = "default" }, transaction: tx).Single().Id;
                queue = CreateJobQueue();
            });
            // Act
            var payload = (SqlFetchedJob)queue.Dequeue(
                DefaultQueues,
                CreateTimingOutCancellationToken());

            // Assert
            Assert.Equal(id, payload.Id);
            Assert.Equal("1", payload.JobId);
            Assert.Equal("default", payload.Queue);
            
        }

        [Fact, CleanDatabase("HangFire.Job", "HangFire.JobQueue")]
        public void Dequeue_ShouldLeaveJobInTheQueue_ButSetItsFetchedAtValue()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                connection.Execute(
                    arrangeSql,
                    new { invocationData = "", arguments = "", queue = "default" }, transaction: tx);
            });
            var queue = CreateJobQueue();

            // Act
            var payload = queue.Dequeue(
                DefaultQueues,
                CreateTimingOutCancellationToken());

            // Assert
            Assert.NotNull(payload);
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                var fetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId = @id",
                    new { id = payload.JobId }, tx).Single();
                Assert.NotNull(fetchedAt);
                Assert.True(fetchedAt > DateTime.UtcNow.AddMinutes(-1));
            });
        }

        [Fact, CleanDatabase("HangFire.Job", "HangFire.JobQueue")]
        public void Dequeue_ShouldFetchATimedOutJobs_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue, FetchedAt)
values (scope_identity(), @queue, @fetchedAt)";

            // Arrange
            SqlJobQueue queue = null;
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                connection.Execute(
                    arrangeSql,
                    new
                    {
                        queue = "default",
                        fetchedAt = DateTime.UtcNow.AddDays(-1),
                        invocationData = "",
                        arguments = ""
                    }, tx);
                queue = CreateJobQueue();
            });
            // Act
            var payload = queue.Dequeue(
                DefaultQueues,
                CreateTimingOutCancellationToken());

            // Assert
            Assert.NotEmpty(payload.JobId);
        }

        [Fact, CleanDatabase("HangFire.Job", "HangFire.JobQueue")]
        public void Dequeue_ShouldSetFetchedAt_OnlyForTheFetchedJob()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            ConnectionUtils.UseConnection((connection, tx) => connection.Execute(
                arrangeSql,
                new[]
                {
                    new { queue = "default", invocationData = "", arguments = "" },
                    new { queue = "default", invocationData = "", arguments = "" }
                },tx));
            var queue = CreateJobQueue();

            // Act
            var payload = queue.Dequeue(
                DefaultQueues,
                CreateTimingOutCancellationToken());

                // Assert
            ConnectionUtils.UseConnection((connection, tx) =>
            {
                var otherJobFetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId != @id",
                    new { id = payload.JobId },tx).Single();

                Assert.Null(otherJobFetchedAt);
            });
        }

        [Fact, CleanDatabase("HangFire.Job", "HangFire.JobQueue")]
        public void Dequeue_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            SqlJobQueue queue = CreateJobQueue();
            ConnectionUtils.UseConnection((connection, tx) => connection.Execute(
                arrangeSql,
                new { queue = "critical", invocationData = "", arguments = "" }, tx));
    
            Assert.Throws<OperationCanceledException>(
                () => queue.Dequeue(
                    DefaultQueues,
                    CreateTimingOutCancellationToken()));
        }

        [Fact, CleanDatabase("HangFire.Job", "HangFire.JobQueue")]
        public void Dequeue_ShouldFetchJobs_FromMultipleQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            ConnectionUtils.UseConnection((connection, tx) =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "critical", invocationData = "", arguments = "" }
                    },tx);
            });
            var queue = CreateJobQueue();

            var critical = (SqlFetchedJob)queue.Dequeue(
                new[] { "critical", "default" },
                CreateTimingOutCancellationToken());

            Assert.NotNull(critical.JobId);
            Assert.Equal("critical", critical.Queue);

            var @default = (SqlFetchedJob)queue.Dequeue(
                new[] { "critical", "default" },
                CreateTimingOutCancellationToken());

            Assert.NotNull(@default.JobId);
            Assert.Equal("default", @default.Queue);
        }

        [Fact, CleanDatabase("HangFire.JobQueue")]
        public void Enqueue_AddsAJobToTheQueue() {
            SqlJobQueue queue = CreateJobQueue();
            queue.Enqueue("default", "1");

            ConnectionUtils.UseConnection((connection, tx) => {
                var record = connection.Query("select * from HangFire.JobQueue", transaction:tx).Single();
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

        private static SqlJobQueue CreateJobQueue()
        {
            return new SqlJobQueue(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), new SqlStorageOptions());
        }
    }
}
