extern alias ReferencedDapper;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using ReferencedDapper::Dapper;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;
// ReSharper disable PossibleNullReferenceException

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerConnectionFacts
    {
        private readonly Mock<IPersistentJobQueue> _queue;
        private readonly PersistentJobQueueProviderCollection _providers;

        public SqlServerConnectionFacts()
        {
            _queue = new Mock<IPersistentJobQueue>();

            var provider = new Mock<IPersistentJobQueueProvider>();
            provider.Setup(x => x.GetJobQueue())
                .Returns(_queue.Object);

            _providers = new PersistentJobQueueProviderCollection(provider.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerConnection(null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void FetchNextJob_DelegatesItsExecution_ToTheQueue(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var token = new CancellationToken();
                var queues = new[] { "default" };

                connection.FetchNextJob(queues, token);

                _queue.Verify(x => x.Dequeue(queues, token));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void FetchNextJob_Throws_IfMultipleProvidersResolved(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var token = new CancellationToken();
                var anotherProvider = new Mock<IPersistentJobQueueProvider>();
                _providers.Add(anotherProvider.Object, new [] { "critical" });

                Assert.Throws<InvalidOperationException>(
                    () => connection.FetchNextJob(new[] { "critical", "default" }, token));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void CreateWriteTransaction_ReturnsNonNullInstance(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var transaction = connection.CreateWriteTransaction();
                Assert.NotNull(transaction);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AcquireLock_ReturnsNonNullInstance(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var @lock = connection.AcquireDistributedLock("1", TimeSpan.FromSeconds(1));
                Assert.NotNull(@lock);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AcquireDistributedLock_ThrowsAnException_WhenResourceIsNullOrEmpty(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                () => connection.AcquireDistributedLock("", TimeSpan.FromMinutes(5)));

                Assert.Equal("resource", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AcquireDistributedLock_AcquiresExclusiveApplicationLock_OnSession(bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                using (connection.AcquireDistributedLock("hello", TimeSpan.FromMinutes(5)))
                {
                    var lockMode = sql.Query<string>(
                        $"select applock_mode('public', '{Constants.DefaultSchema}:hello', 'session')").Single();

                    Assert.Equal("Exclusive", lockMode);
                }
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AcquireDistributedLock_ThrowsAnException_IfLockCanNotBeGranted(bool useMicrosoftDataSqlClient)
        {
            var releaseLock = new ManualResetEventSlim(false);
            var lockAcquired = new ManualResetEventSlim(false);

            var thread = new Thread(
                () => UseConnection(connection1 =>
                {
                    using (connection1.AcquireDistributedLock("exclusive", TimeSpan.FromSeconds(5)))
                    {
                        lockAcquired.Set();
                        releaseLock.Wait();
                    }
                }, useMicrosoftDataSqlClient));
            thread.Start();

            lockAcquired.Wait();

            UseConnection(connection2 =>
            {
                Assert.Throws<DistributedLockTimeoutException>(
                    () =>
                    {
                        using (connection2.AcquireDistributedLock("exclusive", TimeSpan.FromSeconds(5)))
                        {
                        }
                    });
            }, useMicrosoftDataSqlClient);

            releaseLock.Set();
            thread.Join();
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AcquireDistributedLock_Dispose_ReleasesExclusiveApplicationLock(bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var distributedLock = connection.AcquireDistributedLock("hello", TimeSpan.FromMinutes(5));
                distributedLock.Dispose();

                var lockMode = sql.Query<string>(
                    "select applock_mode('public', 'hello', 'session')").Single();

                Assert.Equal("NoLock", lockMode);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AcquireDistributedLock_IsReentrant_FromTheSameConnection_OnTheSameResource(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                using (connection.AcquireDistributedLock("hello", TimeSpan.FromMinutes(5)))
                using (connection.AcquireDistributedLock("hello", TimeSpan.FromMinutes(5)))
                {
                    Assert.True(true);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void CreateExpiredJob_ThrowsAnException_WhenJobIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.CreateExpiredJob(
                        null,
                        new Dictionary<string, string>(),
                        DateTime.UtcNow,
                        TimeSpan.Zero));

                Assert.Equal("job", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void CreateExpiredJob_ThrowsAnException_WhenParametersCollectionIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.CreateExpiredJob(
                        Job.FromExpression(() => SampleMethod("hello")),
                        null,
                        DateTime.UtcNow,
                        TimeSpan.Zero));

                Assert.Equal("parameters", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CreatesAJobInTheStorage_AndSetsItsParameters(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" } },
                    createdAt,
                    TimeSpan.FromDays(1));

                Assert.NotNull(jobId);
                Assert.NotEmpty(jobId);

                var sqlJob = sql.Query($"select * from [{Constants.DefaultSchema}].Job").Single();
                Assert.Equal(jobId, sqlJob.Id.ToString());
                Assert.Equal(createdAt, sqlJob.CreatedAt);
                Assert.Equal(null, (int?) sqlJob.StateId);
                Assert.Equal(null, (string) sqlJob.StateName);

                var invocationData = InvocationData.DeserializePayload((string)sqlJob.InvocationData);
                invocationData.Arguments = sqlJob.Arguments;

                var job = invocationData.Deserialize();
                Assert.Equal(typeof(SqlServerConnectionFacts), job.Type);
                Assert.Equal("SampleMethod", job.Method.Name);
                Assert.Equal("Hello", job.Args[0]);

                Assert.True(createdAt.AddDays(1).AddMinutes(-1) < sqlJob.ExpireAt);
                Assert.True(sqlJob.ExpireAt < createdAt.AddDays(1).AddMinutes(1));

                var parameters = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id",
                    new { id = jobId })
                    .ToDictionary(x => (string) x.Name, x => (string) x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
                Assert.Equal("Value2", parameters["Key2"]);
                Assert.Equal("Value3", parameters["Key3"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateParametersWithNonNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", "Value1" } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateTwoParametersWithNonNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
                Assert.Equal("Value2", parameters["Key2"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateThreeParametersWithNonNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
                Assert.Equal("Value2", parameters["Key2"]);
                Assert.Equal("Value3", parameters["Key3"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateFourParametersWithNonNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" }, { "Key4", "Value4" } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
                Assert.Equal("Value2", parameters["Key2"]);
                Assert.Equal("Value3", parameters["Key3"]);
                Assert.Equal("Value4", parameters["Key4"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateManyParametersWithNonNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" }, { "Key4", "Value4" }, { "Key5", "Value5" } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
                Assert.Equal("Value2", parameters["Key2"]);
                Assert.Equal("Value3", parameters["Key3"]);
                Assert.Equal("Value4", parameters["Key4"]);
                Assert.Equal("Value5", parameters["Key5"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateParametersWithNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", null } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal(null, parameters["Key1"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateTwoParametersWithNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", null }, { "Key2", null } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal(null, parameters["Key1"]);
                Assert.Equal(null, parameters["Key2"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateThreeParametersWithNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", null }, { "Key2", null }, { "Key3", null } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal(null, parameters["Key1"]);
                Assert.Equal(null, parameters["Key2"]);
                Assert.Equal(null, parameters["Key3"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateFourParametersWithNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", null }, { "Key2", null }, { "Key3", null }, { "Key4", null } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal(null, parameters["Key1"]);
                Assert.Equal(null, parameters["Key2"]);
                Assert.Equal(null, parameters["Key3"]);
                Assert.Equal(null, parameters["Key4"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateManyParametersWithNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string> { { "Key1", null }, { "Key2", null }, { "Key3", null }, { "Key4", null }, { "Key5", null } },
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Equal(null, parameters["Key1"]);
                Assert.Equal(null, parameters["Key2"]);
                Assert.Equal(null, parameters["Key3"]);
                Assert.Equal(null, parameters["Key4"]);
                Assert.Equal(null, parameters["Key5"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void CreateExpiredJob_CanCreateJobWithoutParameters(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string>(), 
                    createdAt,
                    TimeSpan.FromDays(1));

                var parameters = sql
                    .Query($"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id", new { id = jobId })
                    .ToDictionary(x => (string)x.Name, x => (string)x.Value);

                Assert.Empty(parameters);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobData_ThrowsAnException_WhenJobIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(() => connection.GetJobData(null)),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobData_ReturnsNull_WhenThereIsNoSuchJob(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetJobData("1");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }
        
        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobData_ReturnsNull_WhenIdentifierCanNotBeParsedAsLong(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetJobData("some-non-long-id");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobData_ReturnsResult_WhenJobExists(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, StateName, CreatedAt)
values (@invocationData, @arguments, @stateName, getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = Job.FromExpression(() => SampleMethod("wrong"));

                var jobId = sql.Query(
                    arrangeSql,
                    new
                    {
                        invocationData = JobHelper.ToJson(InvocationData.Serialize(job)),
                        stateName = "Succeeded",
                        arguments = "['Arguments']"
                    }).Single();

                var result = connection.GetJobData(((long)jobId.Id).ToString());

                Assert.NotNull(result);
                Assert.NotNull(result.Job);
                Assert.Equal("Succeeded", result.State);
                Assert.Equal("Arguments", result.Job.Args[0]);
                Assert.Null(result.LoadException);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < result.CreatedAt);
                Assert.True(result.CreatedAt < DateTime.UtcNow.AddMinutes(1));
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetStateData_ThrowsAnException_WhenJobIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(() => connection.GetStateData(null)),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetStateData_ReturnsNull_IfThereIsNoSuchState(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetStateData("1");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetStateData_ReturnsNull_WhenIdentifierCanNotBeParsedAsLong(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetStateData("some-non-long-id");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetStateData_ReturnsCorrectData(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, StateName, CreatedAt)
values ('', '', '', getutcdate());
declare @JobId bigint;
set @JobId = scope_identity();
insert into [{Constants.DefaultSchema}].State (JobId, Name, CreatedAt)
values (@JobId, 'old-state', getutcdate());
insert into [{Constants.DefaultSchema}].State (JobId, Name, Reason, Data, CreatedAt)
values (@JobId, @name, @reason, @data, getutcdate());
declare @StateId bigint;
set @StateId = scope_identity();
update [{Constants.DefaultSchema}].Job set StateId = @StateId;
select @JobId as Id;";

            UseConnections((sql, connection) =>
            {
                var data = new Dictionary<string, string>
                {
                    { "Key", "Value" }
                };

                var jobId = (long)sql.Query(
                    arrangeSql,
                    new { name = "Name", reason = "Reason", @data = JobHelper.ToJson(data) }).Single().Id;

                var result = connection.GetStateData(jobId.ToString());

                Assert.NotNull(result);

                Assert.Equal("Name", result.Name);
                Assert.Equal("Reason", result.Reason);
                Assert.Equal("Value", result.Data["Key"]);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetStateData_ReturnsCorrectData_WhenPropertiesAreCamelcased(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, StateName, CreatedAt)
values ('', '', '', getutcdate());
declare @JobId bigint;
set @JobId = scope_identity();
insert into [{Constants.DefaultSchema}].State (JobId, Name, CreatedAt)
values (@JobId, 'old-state', getutcdate());
insert into [{Constants.DefaultSchema}].State (JobId, Name, Reason, Data, CreatedAt)
values (@JobId, @name, @reason, @data, getutcdate());
declare @StateId bigint;
set @StateId = scope_identity();
update [{Constants.DefaultSchema}].Job set StateId = @StateId;
select @JobId as Id;";

            UseConnections((sql, connection) =>
            {
                var data = new Dictionary<string, string>
                {
                    { "key", "Value" }
                };

                var jobId = (long)sql.Query(
                    arrangeSql,
                    new { name = "Name", reason = "Reason", @data = JobHelper.ToJson(data) }).Single().Id;

                var result = connection.GetStateData(jobId.ToString());
                Assert.NotNull(result);

                Assert.Equal("Value", result.Data["Key"]);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobData_ReturnsJobLoadException_IfThereWasADeserializationException(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, StateName, CreatedAt)
values (@invocationData, @arguments, @stateName, getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var jobId = sql.Query(
                    arrangeSql,
                    new
                    {
                        invocationData = JobHelper.ToJson(new InvocationData(null, null, null, null)),
                        stateName = "Succeeded",
                        arguments = "['Arguments']"
                    }).Single();

                var result = connection.GetJobData(((long)jobId.Id).ToString());

                Assert.NotNull(result.LoadException);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetParameter_ThrowsAnException_WhenJobIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.SetJobParameter(null, "name", "value"));

                Assert.Equal("id", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetParameter_ThrowsAnException_WhenNameIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.SetJobParameter("1", null, "value"));

                Assert.Equal("name", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetParameters_CreatesNewParameter_WhenParameterWithTheGivenNameDoesNotExists(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = sql.Query(arrangeSql).Single();
                string jobId = job.Id.ToString();

                connection.SetJobParameter(jobId, "Name", "Value");

                var parameter = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id and Name = @name",
                    new { id = jobId, name = "Name" }).Single();

                Assert.Equal("Value", parameter.Value);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetParameter_UpdatesValue_WhenParameterWithTheGivenName_AlreadyExists(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = sql.Query(arrangeSql).Single();
                string jobId = job.Id.ToString();

                connection.SetJobParameter(jobId, "Name", "Value");
                connection.SetJobParameter(jobId, "Name", "AnotherValue");

                var parameter = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id and Name = @name",
                    new { id = jobId, name = "Name" }).Single();

                Assert.Equal("AnotherValue", parameter.Value);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetParameter_CanAcceptNulls_AsValues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = sql.Query(arrangeSql).Single();
                string jobId = job.Id.ToString();

                connection.SetJobParameter(jobId, "Name", null);

                var parameter = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id and Name = @name",
                    new { id = jobId, name = "Name" }).Single();

                Assert.Equal((string) null, parameter.Value);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobParameter_WithIgnoreDupKeyOption_InsertsNonExistingValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnections((sql, connection) =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[JobParameter] REBUILD WITH (IGNORE_DUP_KEY = ON)");

                    string jobId = sql.Query($@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt) values ('', '', getutcdate())
select scope_identity() as Id").Single().Id.ToString();

                    connection.SetJobParameter(jobId, "Name", "Value");

                    var parameter = sql.Query(
                        $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @jobId and Name = N'Name'",
                        new { jobId }).Single();

                    Assert.Equal("Value", parameter.Value);
                }, useBatching, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnections((sql, _) => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[JobParameter] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useBatching, useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobParameter_WithIgnoreDupKeyOption_UpdatesExistingValue_WhenIgnoreDupKeyOptionIsSet(bool setIgnoreDupKey, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnections((sql, connection) =>
                {
                    sql.Execute("SET XACT_ABORT ON");
                    var onOrOff = setIgnoreDupKey ? "ON" : "OFF";
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[JobParameter] REBUILD WITH (IGNORE_DUP_KEY = {onOrOff})");

                    string jobId1 = sql.Query($@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt) values ('', '', getutcdate())
select scope_identity() as Id").Single().Id.ToString();

                    string jobId2 = sql.Query($@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt) values ('', '', getutcdate())
select scope_identity() as Id").Single().Id.ToString();

                    sql.Execute(
                        $@"insert into [{Constants.DefaultSchema}].[JobParameter] (JobId, Name, Value) values
(@jobId1, N'Name1', N'Value1'),
(@jobId1, N'Name2', N'Value1'),
(@jobId2, N'Name1', N'Value1')", new { jobId1, jobId2 });

                    connection.SetJobParameter(jobId1, "Name1", "Value2");

                    var parameters1 = sql.Query(
                        $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @jobId1",
                        new { jobId1 }).ToDictionary(x => (string)x.Name, x => (string)x.Value);

                    Assert.Equal("Value2", parameters1["Name1"]);
                    Assert.Equal("Value1", parameters1["Name2"]);

                    var parameters2 = sql.Query(
                        $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @jobId2",
                        new { jobId2 }).ToDictionary(x => (string)x.Name, x => (string)x.Value);

                    Assert.Equal("Value1", parameters2["Name1"]);
                }, useBatching: false, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnections((sql, _) => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[JobParameter] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useBatching: false, useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetParameter_ThrowsAnException_WhenJobIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetJobParameter(null, "hello"));

                Assert.Equal("id", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetParameter_ThrowsAnException_WhenNameIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetJobParameter("1", null));

                Assert.Equal("name", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetParameter_ReturnsNull_WhenParameterDoesNotExists(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var value = connection.GetJobParameter("1", "hello");
                Assert.Null(value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetParameter_ReturnsNull_WhenJobIdCanNotBeParsedAsLong(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetJobParameter("some-non-long-id", "name");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetParameter_ReturnsParameterValue_WhenJobExists(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
declare @id bigint
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
set @id = scope_identity()
insert into [{Constants.DefaultSchema}].JobParameter (JobId, Name, Value)
values (@id, @name, @value)
select @id";

            UseConnections((sql, connection) =>
            {
                var id = sql.Query<long>(
                    arrangeSql,
                    new { name = "name", value = "value" }).Single();

                var value = connection.GetJobParameter(id.ToString(), "name");

                Assert.Equal("value", value);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetFirstByLowestScoreFromSet(null, 0, 1));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_ToScoreIsLowerThanFromScore(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentException>(
                    () => connection.GetFirstByLowestScoreFromSet("key", 0, -1));

                Assert.Equal("toScore", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ReturnsNull_WhenTheKeyDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetFirstByLowestScoreFromSet(
                    "key", 0, 1);

                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ThrowsArgException_WhenRequestingLessThanZero(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentException>(
                    () => connection.GetFirstByLowestScoreFromSet("key", 0, 1, -1));

                Assert.Equal("count", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ReturnsEmpty_WhenNoneExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetFirstByLowestScoreFromSet("key", 0, 1, 10);
                Assert.Empty(result);
            }, useMicrosoftDataSqlClient);
        }
        
        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ReturnsN_WhenMoreThanNExist(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], Score, Value)
values 
('key', 1.0, '1234'),
('key', -1.0, '567'),
('key', -5.0, '890'),
('another-key', -2.0, 'abcd')";
            
            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);
                
                var result = connection.GetFirstByLowestScoreFromSet("key", -10.0, 10.0, 2);
                
                Assert.Equal(2, result.Count);
                Assert.Equal("890", result.ElementAt(0));
                Assert.Equal("567", result.ElementAt(1));
            }, useBatching: false, useMicrosoftDataSqlClient);
        }
        
        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ReturnsN_WhenMoreThanNExist_And_RequestedCountIsGreaterThanN(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], Score, Value)
values 
('key', 1.0, '1234'),
('key', -1.0, '567'),
('key', -5.0, '890'),
('another-key', -2.0, 'abcd')";
            
            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);
                
                var result = connection.GetFirstByLowestScoreFromSet("another-key", -10.0, 10.0, 5);
                
                Assert.Equal(1, result.Count);
                Assert.Equal("abcd", result.First());
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetFirstByLowestScoreFromSet_ReturnsTheValueWithTheLowestScore(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], Score, Value)
values 
('key', 1.0, '1.0'),
('key', -1.0, '-1.0'),
('key', -5.0, '-5.0'),
('another-key', -2.0, '-2.0')";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);

                var result = connection.GetFirstByLowestScoreFromSet("key", -1.0, 3.0);
                
                Assert.Equal("-1.0", result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AnnounceServer_ThrowsAnException_WhenServerIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.AnnounceServer(null, new ServerContext()));

                Assert.Equal("serverId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AnnounceServer_ThrowsAnException_WhenContextIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.AnnounceServer("server", null));

                Assert.Equal("context", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void AnnounceServer_CreatesOrUpdatesARecord(bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                var context1 = new ServerContext
                {
                    Queues = new[] { "critical", "default" },
                    WorkerCount = 4
                };
                connection.AnnounceServer("server", context1);

                var server = sql.Query($"select * from [{Constants.DefaultSchema}].Server").Single();
                Assert.Equal("server", server.Id);
                Assert.True(((string)server.Data).StartsWith(
                    "{\"WorkerCount\":4,\"Queues\":[\"critical\",\"default\"],\"StartedAt\":"),
                    server.Data);
                Assert.NotNull(server.LastHeartbeat);

                var context2 = new ServerContext
                {
                    Queues = new[] { "default" },
                    WorkerCount = 1000 
                };
                connection.AnnounceServer("server", context2);
                var sameServer = sql.Query($"select * from [{Constants.DefaultSchema}].Server").Single();
                Assert.Equal("server", sameServer.Id);
                Assert.Contains("1000", sameServer.Data);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveServer_ThrowsAnException_WhenServerIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(() => connection.RemoveServer(null)),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveServer_RemovesAServerRecord(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Server (Id, Data, LastHeartbeat)
values 
('Server1', '', getutcdate()),
('Server2', '', getutcdate())";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);

                connection.RemoveServer("Server1");

                var server = sql.Query($"select * from [{Constants.DefaultSchema}].Server").Single();
                Assert.NotEqual("Server1", server.Id, StringComparer.OrdinalIgnoreCase);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Heartbeat_ThrowsAnException_WhenServerIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(() => connection.Heartbeat(null)),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Heartbeat_UpdatesLastHeartbeat_OfTheServerWithGivenId(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Server (Id, Data, LastHeartbeat)
values
('server1', '', '2012-12-12 12:12:12'),
('server2', '', '2012-12-12 12:12:12')";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);

                connection.Heartbeat("server1");

                var servers = sql.Query($"select * from [{Constants.DefaultSchema}].Server")
                    .ToDictionary(x => (string)x.Id, x => (DateTime)x.LastHeartbeat);

                Assert.NotEqual(2012, servers["server1"].Year);
                Assert.Equal(2012, servers["server2"].Year);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveTimedOutServers_ThrowsAnException_WhenTimeOutIsNegative(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentException>(() => connection.RemoveTimedOutServers(TimeSpan.FromMinutes(-5))),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveTimedOutServers_DoItsWorkPerfectly(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Server (Id, Data, LastHeartbeat)
values (@id, '', @heartbeat)";

            UseConnections((sql, connection) =>
            {
                sql.Execute(
                    arrangeSql,
                    new[]
                    {
                        new { id = "server1", heartbeat = DateTime.UtcNow.AddDays(-1) },
                        new { id = "server2", heartbeat = DateTime.UtcNow.AddHours(-12) }
                    });

                connection.RemoveTimedOutServers(TimeSpan.FromHours(15));

                var liveServer = sql.Query($"select * from [{Constants.DefaultSchema}].Server").Single();
                Assert.Equal("server2", liveServer.Id);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllItemsFromSet_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(() => connection.GetAllItemsFromSet(null)),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllItemsFromSet_ReturnsEmptyCollection_WhenKeyDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetAllItemsFromSet("some-set");

                Assert.NotNull(result);
                Assert.Equal(0, result.Count);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllItemsFromSet_ReturnsAllItems(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], Score, Value)
values (@key, 0.0, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "some-set", value = "1" },
                    new { key = "some-set", value = "2" },
                    new { key = "another-set", value = "3" }
                });

                // Act
                var result = connection.GetAllItemsFromSet("some-set");

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Contains("1", result);
                Assert.Contains("2", result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.SetRangeInHash(null, new Dictionary<string, string>()));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.SetRangeInHash("some-hash", null));

                Assert.Equal("keyValuePairs", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetRangeInHash_ThrowsSqlException_WhenKeyIsTooLong(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.ThrowsAny<DbException>(
                    () => connection.SetRangeInHash(
                    "123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_12345",
                    new Dictionary<string, string> { { "field", "value" } }));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_MergesAllRecords(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                connection.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                });

                var result = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].Hash where [Key] = @key",
                    new { key = "some-hash" })
                    .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_CanCreateFieldsWithNullValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                connection.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", null }
                });

                var result = sql.Query(
                        $"select * from [{Constants.DefaultSchema}].Hash where [Key] = @key",
                        new { key = "some-hash" })
                    .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                Assert.Equal(null, result["Key1"]);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_ReleasesTheAcquiredLock(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                connection.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key", "Value" }
                });

                var result = sql.QuerySingle<string>($"select APPLOCK_MODE( 'public' , 'HangFire:Hash:Lock' , 'Session' )");
                Assert.Equal("NoLock", result);
            }, useBatching, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_WithIgnoreDupKeyOption_InsertsNonExistingValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnections((sql, connection) =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = ON)");

                    connection.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key", "value" }
                    });

                    var result = sql
                        .Query($"select * from [{Constants.DefaultSchema}].Hash where [Key] = N'some-hash'")
                        .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                    Assert.Equal("value", result["key"]);
                }, useBatching, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnections((sql, _) => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useBatching, useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false, false), InlineData(false, false, true)]
        [InlineData(false,  true, false), InlineData(false,  true, true)]
        [InlineData( true, false, false), InlineData( true, false, true)]
        [InlineData( true,  true, false), InlineData( true,  true, true)]
        public void SetRangeInHash_WithIgnoreDupKeyOption_UpdatesExistingValue_WhenIgnoreDupKeyOptionIsSet(bool setIgnoreDupKey, bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnections((sql, connection) =>
                {
                    var onOrOff = setIgnoreDupKey ? "ON" : "OFF";
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = {onOrOff})");
                    sql.Execute($@"insert into [{Constants.DefaultSchema}].Hash([Key], Field, Value) VALUES
(N'some-hash', N'key1', N'value1'),
(N'some-hash', N'key2', N'value1'),
(N'othr-hash', N'key1', N'value1')");

                    connection.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key1", "value2" }
                    });

                    var someResult = sql
                        .Query($"select * from [{Constants.DefaultSchema}].Hash where [Key] = N'some-hash'")
                        .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                    Assert.Equal("value2", someResult["key1"]);
                    Assert.Equal("value1", someResult["key2"]);

                    var othrResult = sql
                        .Query($"select * from [{Constants.DefaultSchema}].Hash where [Key] = N'othr-hash'")
                        .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                    Assert.Equal("value1", othrResult["key1"]);
                }, useBatching, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnections((sql, _) => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useBatching, useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllEntriesFromHash_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(() => connection.GetAllEntriesFromHash(null)),
                useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllEntriesFromHash_ReturnsNull_IfHashDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetAllEntriesFromHash("some-hash");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllEntriesFromHash_ReturnsAllKeysAndTheirValues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field], [Value])
values (@key, @field, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "some-hash", field = "Key1", value = "Value1" },
                    new { key = "some-hash", field = "Key2", value = "Value2" },
                    new { key = "another-hash", field = "Key3", value = "Value3" }
                });

                // Act
                var result = connection.GetAllEntriesFromHash("some-hash");

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetSetCount_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetSetCount(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetSetCount_ReturnsZero_WhenSetDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetSetCount("my-set");
                Assert.Equal(0, result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetSetCount_ReturnsNumberOfElements_InASet(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Value], [Score])
values (@key, @value, 0.0)";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql, new List<dynamic>
                {
                    new { key = "set-1", value = "value-1" },
                    new { key = "set-2", value = "value-1" },
                    new { key = "set-1", value = "value-2" }
                });

                var result = connection.GetSetCount("set-1");

                Assert.Equal(2, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetRangeFromSet_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(() => connection.GetRangeFromSet(null, 0, 1));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetRangeFromSet_ReturnsPagedElements(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Value], [Score])
values (@Key, @Value, 0.0)";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql, new List<dynamic>
                {
                    new { Key = "set-1", Value = "1" },
                    new { Key = "set-1", Value = "2" },
                    new { Key = "set-1", Value = "3" },
                    new { Key = "set-1", Value = "4" },
                    new { Key = "set-2", Value = "4" },
                    new { Key = "set-1", Value = "5" }
                });

                var result = connection.GetRangeFromSet("set-1", 2, 3);

                Assert.Equal(new [] { "3", "4" }, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetCounter_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetCounter(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetCounter_ReturnsZero_WhenKeyDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetCounter("my-counter");
                Assert.Equal(0, result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetCounter_ReturnsSumOfValues_InCounterTable(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Counter ([Key], [Value])
values (@key, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "counter-1", value = 1 },
                    new { key = "counter-2", value = 1 },
                    new { key = "counter-1", value = 1 }
                });

                // Act
                var result = connection.GetCounter("counter-1");

                // Assert
                Assert.Equal(2, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetCounter_IncludesValues_FromCounterAggregateTable(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].AggregatedCounter ([Key], [Value])
values (@key, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "counter-1", value = 12 },
                    new { key = "counter-2", value = 15 }
                });

                // Act
                var result = connection.GetCounter("counter-1");

                Assert.Equal(12, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetHashCount_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(() => connection.GetHashCount(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetHashCount_ReturnsZero_WhenKeyDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetHashCount("my-hash");
                Assert.Equal(0, result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetHashCount_ReturnsNumber_OfHashFields(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field])
values (@key, @field)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "hash-1", field = "field-1" },
                    new { key = "hash-1", field = "field-2" },
                    new { key = "hash-2", field = "field-1" }
                });

                // Act
                var result = connection.GetHashCount("hash-1");

                // Assert
                Assert.Equal(2, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetHashTtl_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetHashTtl(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetHashTtl_ReturnsNegativeValue_WhenHashDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetHashTtl("my-hash");
                Assert.True(result < TimeSpan.Zero);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetHashTtl_ReturnsExpirationTimeForHash(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field], [ExpireAt])
values (@key, @field, @expireAt)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "hash-1", field = "field", expireAt = (DateTime?)DateTime.UtcNow.AddHours(1) },
                    new { key = "hash-2", field = "field", expireAt = (DateTime?) null }
                });

                // Act
                var result = connection.GetHashTtl("hash-1");

                // Assert
                Assert.True(TimeSpan.FromMinutes(59) < result);
                Assert.True(result < TimeSpan.FromMinutes(61));
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetListCount_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetListCount(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetListCount_ReturnsZero_WhenListDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetListCount("my-list");
                Assert.Equal(0, result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetListCount_ReturnsTheNumberOfListElements(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].List ([Key])
values (@key)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "list-1" },
                    new { key = "list-1" },
                    new { key = "list-2" }
                });

                // Act
                var result = connection.GetListCount("list-1");

                // Assert
                Assert.Equal(2, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetListTtl_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetListTtl(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetListTtl_ReturnsNegativeValue_WhenListDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetListTtl("my-list");
                Assert.True(result < TimeSpan.Zero);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetListTtl_ReturnsExpirationTimeForList(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].List ([Key], [ExpireAt])
values (@key, @expireAt)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "list-1", expireAt = (DateTime?) DateTime.UtcNow.AddHours(1) },
                    new { key = "list-2", expireAt = (DateTime?) null }
                });

                // Act
                var result = connection.GetListTtl("list-1");

                // Assert
                Assert.True(TimeSpan.FromMinutes(59) < result);
                Assert.True(result < TimeSpan.FromMinutes(61));
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetValueFromHash_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetValueFromHash(null, "name"));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetValueFromHash_ThrowsAnException_WhenNameIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetValueFromHash("key", null));

                Assert.Equal("name", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetValueFromHash_ReturnsNull_WhenHashDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetValueFromHash("my-hash", "name");
                Assert.Null(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetValueFromHash_ReturnsValue_OfAGivenField(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field], [Value])
values (@key, @field, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "hash-1", field = "field-1", value = "1" },
                    new { key = "hash-1", field = "field-2", value = "2" },
                    new { key = "hash-2", field = "field-1", value = "3" }
                });

                // Act
                var result = connection.GetValueFromHash("hash-1", "field-1");

                // Assert
                Assert.Equal("1", result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetRangeFromList_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetRangeFromList(null, 0, 1));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetRangeFromList_ReturnsAnEmptyList_WhenListDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetRangeFromList("my-list", 0, 1);
                Assert.Empty(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetRangeFromList_ReturnsAllEntries_WithinGivenBounds(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].List ([Key], [Value])
values (@key, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "list-1", value = "1" },
                    new { key = "list-2", value = "2" },
                    new { key = "list-1", value = "3" },
                    new { key = "list-1", value = "4" },
                    new { key = "list-1", value = "5" }
                });

                // Act
                var result = connection.GetRangeFromList("list-1", 1, 2);
                
                // Assert
                Assert.Equal(new [] { "4", "3" }, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllItemsFromList_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetAllItemsFromList(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllItemsFromList_ReturnsAnEmptyList_WhenListDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetAllItemsFromList("my-list");
                Assert.Empty(result);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetAllItemsFromList_ReturnsAllItems_FromAGivenList(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].List ([Key], Value)
values (@key, @value)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "list-1", value = "1" },
                    new { key = "list-2", value = "2" },
                    new { key = "list-1", value = "3" }
                });

                // Act
                var result = connection.GetAllItemsFromList("list-1");

                // Assert
                Assert.Equal(new [] { "3", "1" }, result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetSetTtl_ThrowsAnException_WhenKeyIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                Assert.Throws<ArgumentNullException>(() => connection.GetSetTtl(null));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetSetTtl_ReturnsNegativeValue_WhenSetDoesNotExist(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var result = connection.GetSetTtl("my-set");
                Assert.True(result < TimeSpan.Zero);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetSetTtl_ReturnsExpirationTime_OfAGivenSet(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Value], [ExpireAt], [Score])
values (@key, @value, @expireAt, 0.0)";

            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "set-1", value = "1", expireAt = (DateTime?) DateTime.UtcNow.AddMinutes(60) },
                    new { key = "set-2", value = "2", expireAt = (DateTime?) null }
                });

                // Act
                var result = connection.GetSetTtl("set-1");

                // Assert
                Assert.True(TimeSpan.FromMinutes(59) < result);
                Assert.True(result < TimeSpan.FromMinutes(61));
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobData_ReturnsResult_WhenJobIdIsLongValue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON;
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, StateName, CreatedAt)
values (@jobId, @invocationData, '[''Arguments'']', 'Succeeded', getutcdate());";

            UseConnections((sql, connection) =>
            {
                var job = Job.FromExpression(() => SampleMethod("hello"));

                sql.Query(
                    arrangeSql,
                    new
                    {
                        jobId = int.MaxValue + 1L,
                        invocationData = JobHelper.ToJson(InvocationData.Serialize(job)),
                    });

                var result = connection.GetJobData((int.MaxValue + 1L).ToString());

                Assert.NotNull(result);
                Assert.NotNull(result.Job);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetStateData_ReturnsCorrectData_WhenJobIdAndStateIdAreLongValues(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, StateName, CreatedAt)
values (@jobId, '', '', '', getutcdate());
insert into [{Constants.DefaultSchema}].State (JobId, Name, CreatedAt)
values (@jobId, 'old-state', getutcdate());
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job OFF
SET IDENTITY_INSERT [{Constants.DefaultSchema}].State ON
insert into [{Constants.DefaultSchema}].State (Id, JobId, Name, Data, CreatedAt)
values (@stateId, @jobId, 'Name', @data, getutcdate());
update [{Constants.DefaultSchema}].Job set StateId = @stateId;";

            UseConnections((sql, connection) =>
            {
                var data = new Dictionary<string, string>
                {
                    { "Key", "Value" }
                };

                sql.Query(
                    arrangeSql,
                    new
                    {
                        jobId = int.MaxValue + 1L,
                        stateId = int.MaxValue + 1L,
                        data = JobHelper.ToJson(data)
                    });

                var result = connection.GetStateData((int.MaxValue + 1L).ToString());

                Assert.NotNull(result);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void CreateExpiredJob_HandlesJobIdCanExceedInt32Max(bool useMicrosoftDataSqlClient)
        {
            UseConnections((sql, connection) =>
            {
                // Arrange
                sql.Query($"DBCC CHECKIDENT('[{Constants.DefaultSchema}].Job', RESEED, {int.MaxValue + 1L});");

                // Act
                var createdAt = new DateTime(2012, 12, 12);
                var jobId = connection.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod("Hello")),
                    new Dictionary<string, string>(),
                    createdAt,
                    TimeSpan.FromDays(1));

                // Assert
                Assert.True(int.MaxValue < long.Parse(jobId));
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void SetJobParameter_CreatesNewParameter_WhenJobIdIsLongValue(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, CreatedAt)
values (@jobId, '', '', getutcdate())";

            UseConnections((sql, connection) =>
            {
                sql.Query(
                    arrangeSql,
                    new { jobId =  int.MaxValue + 1L});

                connection.SetJobParameter((int.MaxValue + 1L).ToString(), "Name", "Value");

                var parameter = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].JobParameter where JobId = @id and Name = @name",
                    new { id = int.MaxValue + 1L, name = "Name" }).Single();

                Assert.NotNull(parameter);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void GetJobParameter_ReturnsParameterValue_WhenJobIdIsLong(bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, CreatedAt)
values (@jobId, '', '', getutcdate())
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job OFF
insert into [{Constants.DefaultSchema}].JobParameter (JobId, Name, Value)
values (@jobId, @name, @value)";

            UseConnections((sql, connection) =>
            {
                sql.Query(
                    arrangeSql, 
                    new
                    {
                        jobId = int.MaxValue + 1L,
                        name = "name", value = "value"
                    });

                var value = connection.GetJobParameter((int.MaxValue + 1L).ToString(), "name");

                Assert.Equal("value", value);
            }, useBatching: false, useMicrosoftDataSqlClient);
        }

        [Fact, CleanSerializerSettings]
        public void HandlesChangingProcessOfStateDataSerialization()
        {
            GlobalConfiguration.Configuration.UseSerializerSettings(SerializerSettingsHelper.DangerousSettings);
            var stateData = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", null }
            };
            var serializedData = SerializationHelper.Serialize(stateData, SerializationOption.User);

            var deserializedStateData = SerializationHelper.Deserialize<Dictionary<string, string>>(serializedData);

            Assert.NotNull(deserializedStateData);
            Assert.Equal(2, deserializedStateData.Count);

            Assert.Equal("value1", deserializedStateData["key1"]);
            Assert.Equal(null, deserializedStateData["key2"]);
        }

        [Fact, CleanSerializerSettings]
        public void HandlesChangingProcessOfInvocationDataSerialization()
        {
            GlobalConfiguration.Configuration.UseSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var initialJob = Job.FromExpression(() => Console.WriteLine());
            var invocationData = InvocationData.Serialize(initialJob);

            var serializedInvocationData = SerializationHelper.Serialize(invocationData, SerializationOption.User);

            var deserializedStateData = SerializationHelper.Deserialize<InvocationData>(serializedInvocationData);
            var deserializedJob = deserializedStateData.Deserialize();

            Assert.Equal(initialJob.Args, deserializedJob.Args);
            Assert.Equal(initialJob.Method, deserializedJob.Method);
            Assert.Equal(initialJob.Type, deserializedJob.Type);
        }

        private void UseConnections(Action<DbConnection, SqlServerConnection> action, bool useBatching, bool useMicrosoftDataSqlClient)
        {
            using (var sqlConnection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                var storage = new SqlServerStorage(sqlConnection, new SqlServerStorageOptions { CommandBatchMaxTimeout = useBatching ? TimeSpan.FromMinutes(1) : (TimeSpan?)null });
                using (var connection = new SqlServerConnection(storage))
                {
                    action(sqlConnection, connection);
                }
            }
        }

        private void UseConnection(Action<SqlServerConnection> action, bool useMicrosoftDataSqlClient)
        {
            using (var sql = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                var storage = new Mock<SqlServerStorage>(sql);
                storage.Setup(x => x.QueueProviders).Returns(_providers);

                using (var connection = new SqlServerConnection(storage.Object))
                {
                    action(connection);
                }
            }
        }

        public static void SampleMethod(string arg){ }
    }
}
