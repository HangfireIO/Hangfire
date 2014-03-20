using System;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class FetcherFacts
    {
        [Fact]
        public void Ctor_ShouldThrowAnException_WhenConnection_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetcher(null, new[] { "default" }));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenQueuesCollectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetcher(new Mock<IDbConnection>().Object, null));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenQueuesCollectionIsEmpty()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new SqlServerFetcher(new Mock<IDbConnection>().Object, new string[0]));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void FetchNextJob_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            var connection = new Mock<IDbConnection>();
            var fetcher = new SqlServerFetcher(connection.Object, new[] { "default" });
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(
                () => fetcher.FetchNextJob(cts.Token));
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var fetcher = new SqlServerFetcher(connection, new[] { "default" });
                var cts = new CancellationTokenSource(200);

                Assert.Throws<OperationCanceledException>(
                    () => fetcher.FetchNextJob(cts.Token));
            }
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            // Arrange
            using (var connection = ConnectionUtils.CreateConnection())
            {
                connection.Execute(
                    arrangeSql,
                    new
                    {
                        invocationData = "{ Type: 'Type', Method: 'Method', ParameterTypes: 'Parameters' }",
                        arguments = "Arguments",
                        queue = "default"
                    });

                var fetcher = new SqlServerFetcher(connection, new[] { "default" });

                // Act
                var payload = fetcher.FetchNextJob(CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.Id);
                Assert.Equal("Type", payload.InvocationData.Type);
                Assert.Equal("Method", payload.InvocationData.Method);
                Assert.Equal("Parameters", payload.InvocationData.ParameterTypes);
                Assert.Equal("default", payload.Queue);
                Assert.Equal("Arguments", payload.Arguments);
            }
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
            using (var connection = ConnectionUtils.CreateConnection())
            {
                connection.Execute(
                    arrangeSql,
                    new { invocationData = "", arguments = "", queue = "default" });

                var fetcher = new SqlServerFetcher(connection, new[] { "default" });

                // Act
                var payload = fetcher.FetchNextJob(CreateTimingOutCancellationToken());

                // Assert
                Assert.NotNull(payload);

                var fetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId = @id",
                    new { id = payload.Id }).Single();

                Assert.NotNull(fetchedAt);
                Assert.True(fetchedAt > DateTime.UtcNow.AddMinutes(-1));
            }
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
            using (var connection = ConnectionUtils.CreateConnection())
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

                var fetcher = new SqlServerFetcher(connection, new[] { "default" });

                // Act
                var payload = fetcher.FetchNextJob(CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.Id);
            }
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
            using (var connection = ConnectionUtils.CreateConnection())
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "default", invocationData = "", arguments = "" }
                    });

                var fetcher = new SqlServerFetcher(connection, new[] { "default" });

                // Act
                var payload = fetcher.FetchNextJob(CreateTimingOutCancellationToken());

                // Assert
                var otherJobFetchedAt = connection.Query<DateTime?>(
                    "select FetchedAt from HangFire.JobQueue where JobId != @id",
                    new { id = payload.Id }).Single();

                Assert.Null(otherJobFetchedAt);
            }
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            using (var connection = ConnectionUtils.CreateConnection())
            {
                connection.Execute(
                    arrangeSql,
                    new { queue = "critical", invocationData = "", arguments = "" });

                var fetcher = new SqlServerFetcher(connection, new[] { "default" });

                Assert.Throws<OperationCanceledException>(
                    () => fetcher.FetchNextJob(CreateTimingOutCancellationToken()));
            }
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldFetchJobs_FromMultipleQueues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values (@invocationData, @arguments, getutcdate())
insert into HangFire.JobQueue (JobId, Queue)
values (scope_identity(), @queue)";

            using (var connection = ConnectionUtils.CreateConnection())
            {
                connection.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { queue = "default", invocationData = "", arguments = "" },
                        new { queue = "critical", invocationData = "", arguments = "" }
                    });

                var fetcher = new SqlServerFetcher(connection, new []{ "critical", "default" });

                var critical = fetcher.FetchNextJob(CreateTimingOutCancellationToken());
                Assert.NotNull(critical.Id);
                Assert.Equal("critical", critical.Queue);

                var @default = fetcher.FetchNextJob(CreateTimingOutCancellationToken());
                Assert.NotNull(@default.Id);
                Assert.Equal("default", @default.Queue);
            }
        }

        [Fact, CleanDatabase]
        public void FetchNextJob_ShouldIgnoreNonExistentJobs_ButDeleteThemFromTheQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue)";

            using (var connection = ConnectionUtils.CreateConnection())
            {
                connection.Execute(arrangeSql, new { id = 1, queue = "default" });

                var fetcher = new SqlServerFetcher(connection, new[] { "default" });

                Assert.Throws<OperationCanceledException>(
                    () => fetcher.FetchNextJob(CreateTimingOutCancellationToken()));

                var queuedJobCount = connection.Query<int>(
                    "select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(0, queuedJobCount);
            }
        }

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(500);
            return source.Token;
        }
    }
}
