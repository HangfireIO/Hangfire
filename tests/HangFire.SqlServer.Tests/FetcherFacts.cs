using System;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using HangFire.Common;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public partial class ConnectionFacts
    {
        private static readonly string[] DefaultQueues = { "default" };

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldThrowAnException_WhenQueuesCollectionIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.FetchNextJob(null, CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldThrowAnException_WhenQueuesCollectionIsEmpty()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentException>(
                    () => connection.FetchNextJob(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void FetchNextJob_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.Throws<OperationCanceledException>(
                    () => connection.FetchNextJob(DefaultQueues, cts.Token));
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource(200);

                Assert.Throws<OperationCanceledException>(
                    () => connection.FetchNextJob(DefaultQueues, cts.Token));
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@jobId, @queue)";

            // Arrange
            UseConnections((connection, storageConnection) =>
            {
                connection.Execute(
                    arrangeSql,
                    new
                    {
                        jobId = 1,
                        queue = "default"
                    });

                // Act
                var payload = storageConnection.FetchNextJob(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal("1", payload.Id);
                Assert.Equal("default", payload.Queue);
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldLeaveJobInTheQueue_ButSetItsFetchedAtValue()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnections((connection, storageConnection) =>
            {
                connection.Execute(
                    arrangeSql,
                    new { invocationData = "", arguments = "", queue = "default" });

                // Act
                var payload = storageConnection.FetchNextJob(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotNull(payload);

                var fetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId = @id",
                    new { id = payload.Id }).Single();

                Assert.NotNull(fetchedAt);
                Assert.True(fetchedAt > DateTime.UtcNow.AddMinutes(-1));
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchATimedOutJobs_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue, FetchedAt)
values (scope_identity(), @queue, @fetchedAt)";

            // Arrange
            UseConnections((connection, storageConnection) =>
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

                // Act
                var payload = storageConnection.FetchNextJob(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.Id);
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldSetFetchedAt_OnlyForTheFetchedJob()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            UseConnections((connection, storageConnection) =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "default", invocationData = "", arguments = "" }
                    });
                // Act
                var payload = storageConnection.FetchNextJob(
                    DefaultQueues,
                    CreateTimingOutCancellationToken());

                // Assert
                var otherJobFetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId != @id",
                    new { id = payload.Id }).Single();

                Assert.Null(otherJobFetchedAt);
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnections((connection, storageConnection) =>
            {
                connection.Execute(
                    arrangeSql,
                    new { queue = "critical", invocationData = "", arguments = "" });
                
                Assert.Throws<OperationCanceledException>(
                    () => storageConnection.FetchNextJob(
                        DefaultQueues,
                        CreateTimingOutCancellationToken()));
            });
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchJobs_FromMultipleQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            UseConnections((connection, storageConnection) =>
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "critical", invocationData = "", arguments = "" }
                    });

                var critical = storageConnection.FetchNextJob(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(critical.Id);
                Assert.Equal("critical", critical.Queue);

                var @default = storageConnection.FetchNextJob(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(@default.Id);
                Assert.Equal("default", @default.Queue);
            });
        }

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return source.Token;
        }

        public static void Sample(string arg1, string arg2) { }
    }
}
