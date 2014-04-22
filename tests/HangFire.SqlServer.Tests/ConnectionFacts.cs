using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using Dapper;
using HangFire.Common;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public partial class ConnectionFacts
    {
        private readonly Mock<JobStorage> _storage;

        public ConnectionFacts()
        {
            _storage = new Mock<JobStorage>();
        }

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SqlServerConnection(null, connection));

                Assert.Equal("storage", exception.ParamName);
            }
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerConnection(_storage.Object, null));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Ctor_CorrectlySets_TheStorageProperty()
        {
            UseConnection(connection => Assert.Same(_storage.Object, connection.Storage));
        }

        [Fact, CleanDatabase]
        public void CreateWriteTransaction_ReturnsNonNullInstance()
        {
            UseConnection(connection =>
            {
                var transaction = connection.CreateWriteTransaction();
                Assert.NotNull(transaction);
            });
        }

        [Fact, CleanDatabase]
        public void AcquireJobLock_ReturnsNonNullInstance()
        {
            UseConnection(connection =>
            {
                var @lock = connection.AcquireJobLock("1");
                Assert.NotNull(@lock);
            });
        }

        [Fact, CleanDatabase]
        public void CreateExpiredJob_ThrowsAnException_WhenInvocationDataIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.CreateExpiredJob(
                        null,
                        new string[0],
                        new Dictionary<string, string>(),
                        TimeSpan.Zero));

                Assert.Equal("invocationData", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void CreateExpiredJob_ThrowsAnException_WhenArgumentsIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.CreateExpiredJob(
                        new InvocationData(null, null, null),
                        null,
                        new Dictionary<string, string>(),
                        TimeSpan.Zero));

                Assert.Equal("arguments", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void CreateExpiredJob_ThrowsANException_WhenParametersCollectionIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.CreateExpiredJob(
                        new InvocationData(null, null, null),
                        new string[0],
                        null,
                        TimeSpan.Zero));

                Assert.Equal("parameters", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void CreateExpiredJob_CreatesAJobInTheStorage_AndSetsItsParameters()
        {
            UseConnections((sql, connection) =>
            {
                var jobId = connection.CreateExpiredJob(
                    new InvocationData("Type", "Method", "Parameters"),
                    new[] { "one", "two" },
                    new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } },
                    TimeSpan.FromDays(1));

                Assert.NotNull(jobId);
                Assert.NotEmpty(jobId);

                var job = sql.Query("select * from HangFire.Job").Single();
                Assert.Equal(jobId, job.Id.ToString());
                Assert.Equal(null, (int?) job.StateId);
                Assert.Equal(null, (string) job.StateName);
                Assert.Equal(
                    "{\"Type\":\"Type\",\"Method\":\"Method\",\"ParameterTypes\":\"Parameters\"}",
                    job.InvocationData);
                Assert.Equal("[\"one\",\"two\"]", job.Arguments);
                Assert.True(DateTime.UtcNow < job.ExpireAt && job.ExpireAt < DateTime.UtcNow.AddDays(1));

                var parameters = sql.Query(
                    "select * from HangFire.JobParameter where JobId = @id",
                    new { id = jobId })
                    .ToDictionary(x => (string) x.Name, x => (string) x.Value);

                Assert.Equal("Value1", parameters["Key1"]);
                Assert.Equal("Value2", parameters["Key2"]);
            });
        }

        [Fact, CleanDatabase]
        public void GetJobData_ThrowsAnException_WhenJobIdIsNull()
        {
            UseConnection(connection => Assert.Throws<ArgumentNullException>(
                    () => connection.GetJobData(null)));
        }

        [Fact, CleanDatabase]
        public void GetJobData_ReturnsNull_WhenThereIsNoSuchJob()
        {
            UseConnection(connection =>
            {
                var result = connection.GetJobData("1");
                Assert.Null(result);
            });
        }

        [Fact, CleanDatabase]
        public void GetJobData_ReturnsResult_WhenJobExists()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, StateName, CreatedAt)
values (@invocationData, @arguments, @stateName, getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var jobId = sql.Query(
                    arrangeSql,
                    new
                    {
                        invocationData = JobHelper.ToJson(MethodData.FromExpression(() => SampleMethod()).Serialize()),
                        stateName = "Succeeded",
                        arguments = "['Arguments']"
                    }).Single();

                var result = connection.GetJobData(((int)jobId.Id).ToString());

                Assert.NotNull(result);
                Assert.NotNull(result.MethodData);
                Assert.Equal("Succeeded", result.State);
                Assert.Equal("Arguments", result.Arguments[0]);
            });
        }

        [Fact, CleanDatabase]
        public void SetParameter_ThrowsAnException_WhenJobIdIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.SetJobParameter(null, "name", "value"));

                Assert.Equal("id", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void SetParameter_ThrowsAnException_WhenNameIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.SetJobParameter("1", null, "value"));

                Assert.Equal("name", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void SetParameters_CreatesNewParameter_WhenParameterWithTheGivenNameDoesNotExists()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = sql.Query(arrangeSql).Single();
                string jobId = job.Id.ToString();

                connection.SetJobParameter(jobId, "Name", "Value");

                var parameter = sql.Query(
                    "select * from HangFire.JobParameter where JobId = @id and Name = @name",
                    new { id = jobId, name = "Name" }).Single();

                Assert.Equal("Value", parameter.Value);
            });
        }

        [Fact, CleanDatabase]
        public void SetParameter_UpdatesValue_WhenParameterWithTheGivenName_AlreadyExists()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = sql.Query(arrangeSql).Single();
                string jobId = job.Id.ToString();

                connection.SetJobParameter(jobId, "Name", "Value");
                connection.SetJobParameter(jobId, "Name", "AnotherValue");

                var parameter = sql.Query(
                    "select * from HangFire.JobParameter where JobId = @id and Name = @name",
                    new { id = jobId, name = "Name" }).Single();

                Assert.Equal("AnotherValue", parameter.Value);
            });
        }

        [Fact, CleanDatabase]
        public void SetParameter_CanAcceptNulls_AsValues()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnections((sql, connection) =>
            {
                var job = sql.Query(arrangeSql).Single();
                string jobId = job.Id.ToString();

                connection.SetJobParameter(jobId, "Name", null);

                var parameter = sql.Query(
                    "select * from HangFire.JobParameter where JobId = @id and Name = @name",
                    new { id = jobId, name = "Name" }).Single();

                Assert.Equal((string) null, parameter.Value);
            });
        }

        [Fact, CleanDatabase]
        public void GetParameter_ThrowsAnException_WhenJobIdIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetJobParameter(null, "hello"));

                Assert.Equal("id", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void GetParameter_ThrowsAnException_WhenNameIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetJobParameter("1", null));

                Assert.Equal("name", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void GetParameter_ReturnsNull_WhenParameterDoesNotExists()
        {
            UseConnection(connection =>
            {
                var value = connection.GetJobParameter("1", "hello");
                Assert.Null(value);
            });
        }

        [Fact, CleanDatabase]
        public void GetParameter_ReturnsParameterValue_WhenJobExists()
        {
            const string arrangeSql = @"
declare @id int
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
set @id = scope_identity()
insert into HangFire.JobParameter (JobId, Name, Value)
values (@id, @name, @value)
select @id";

            UseConnections((sql, connection) =>
            {
                var id = sql.Query<int>(
                    arrangeSql,
                    new { name = "name", value = "value" }).Single();

                var value = connection.GetJobParameter(id.ToString(), "name");

                Assert.Equal("value", value);
            });
        }

        [Fact, CleanDatabase]
        public void DeleteJobFromQueue_ThrowsAnException_WhenIdIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.DeleteJobFromQueue(null, "default"));

                Assert.Equal("id", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void DeleteJobFromQueue_ThrowsAnException_WhenQueueIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.DeleteJobFromQueue("1", null));

                Assert.Equal("queue", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void DeleteJobFromQueue_ReallyDeletesTheJobFromTheQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue)";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql, new { id = "1", queue = "default" });

                connection.DeleteJobFromQueue("1", "default");

                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(0, count);
            });
        }

        [Fact, CleanDatabase]
        public void DeleteJobFromQueue_DoesNotDelete_UnrelatedJobs()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue)";

            UseConnections((sql, connection) =>
            {
                sql.Execute(
                    arrangeSql,
                    new[]
                    { 
                        new { id = "1", queue = "critical" },
                        new { id = "2", queue = "default" } 
                    }); 

                connection.DeleteJobFromQueue("1", "default");

                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(2, count);
            });
        }

        [Fact, CleanDatabase]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetFirstByLowestScoreFromSet(null, 0, 1));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_ToScoreIsLowerThanFromScore()
        {
            UseConnection(connection => Assert.Throws<ArgumentException>(
                () => connection.GetFirstByLowestScoreFromSet("key", 0, -1)));
        }

        [Fact, CleanDatabase]
        public void GetFirstByLowestScoreFromSet_ReturnsNull_WhenTheKeyDoesNotExist()
        {
            UseConnection(connection =>
            {
                var result = connection.GetFirstByLowestScoreFromSet(
                    "key", 0, 1);

                Assert.Null(result);
            });
        }

        [Fact, CleanDatabase]
        public void GetFirstByLowestScoreFromSet_ReturnsTheValueWithTheLowestScore()
        {
            const string arrangeSql = @"
insert into HangFire.[Set] ([Key], Score, Value)
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
            });
        }

        [Fact, CleanDatabase]
        public void AnnounceServer_ThrowsAnException_WhenServerIdIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.AnnounceServer(null, 0, new string[0]));

                Assert.Equal("serverId", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void AnnounceServer_ThrowsAnException_WhenQueuesCollectionIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.AnnounceServer("server", 0, null));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void AnnounceServer_CreatesOrUpdatesARecord()
        {
            UseConnections((sql, connection) =>
            {
                connection.AnnounceServer("server", 4, new [] { "critical", "default" });

                var server = sql.Query("select * from HangFire.Server").Single();
                Assert.Equal("server", server.Id);
                Assert.True(((string)server.Data).StartsWith(
                    "{\"WorkerCount\":4,\"Queues\":[\"critical\",\"default\"],\"StartedAt\":"),
                    server.Data);
                Assert.Null(server.HeartBeat);

                connection.AnnounceServer("server", 1000, new [] { "default" });
                var sameServer = sql.Query("select * from HangFire.Server").Single();
                Assert.Equal("server", sameServer.Id);
                Assert.Contains("1000", sameServer.Data);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveServer_ThrowsAnException_WhenServerIdIsNull()
        {
            UseConnection(connection => Assert.Throws<ArgumentNullException>(
                () => connection.RemoveServer(null)));
        }

        [Fact, CleanDatabase]
        public void RemoveServer_RemovesAServerRecord()
        {
            const string arrangeSql = @"
insert into HangFire.Server (Id, Data)
values 
('Server1', ''),
('Server2', '')";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);

                connection.RemoveServer("Server1");

                var server = sql.Query("select * from HangFire.Server").Single();
                Assert.NotEqual("Server1", server.Id, StringComparer.OrdinalIgnoreCase);
            });
        }

        [Fact, CleanDatabase]
        public void Heartbeat_ThrowsAnException_WhenServerIdIsNull()
        {
            UseConnection(connection => Assert.Throws<ArgumentNullException>(
                () => connection.Heartbeat(null)));
        }

        [Fact, CleanDatabase]
        public void Heartbeat_UpdatesLastHeartbeat_OfTheServerWithGivenId()
        {
            const string arrangeSql = @"
insert into HangFire.Server (Id, Data)
values
('server1', ''),
('server2', '')";

            UseConnections((sql, connection) =>
            {
                sql.Execute(arrangeSql);

                connection.Heartbeat("server1");

                var servers = sql.Query("select * from HangFire.Server")
                    .ToDictionary(x => (string)x.Id, x => (DateTime?)x.LastHeartbeat);

                Assert.NotNull(servers["server1"]);
                Assert.Null(servers["server2"]);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveTimedOutServers_ThrowsAnException_WhenTimeOutIsNegative()
        {
            UseConnection(connection => Assert.Throws<ArgumentException>(
                () => connection.RemoveTimedOutServers(TimeSpan.FromMinutes(-5))));
        }

        [Fact, CleanDatabase]
        public void RemoveTimedOutServers_DoItsWorkPerfectly()
        {
            const string arrangeSql = @"
insert into HangFire.Server (Id, Data, LastHeartbeat)
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

                var liveServer = sql.Query("select * from HangFire.Server").Single();
                Assert.Equal("server2", liveServer.Id);
            });
        }

        private void UseConnections(Action<SqlConnection, SqlServerConnection> action)
        {
            using (var sqlConnection = ConnectionUtils.CreateConnection())
            using (var connection = new SqlServerConnection(_storage.Object, sqlConnection))
            {
                action(sqlConnection, connection);
            }
        }

        private void UseConnection(Action<SqlServerConnection> action)
        {
            using (var connection = new SqlServerConnection(
                _storage.Object, ConnectionUtils.CreateConnection()))
            {
                action(connection);
            }
        }

        public static void SampleMethod(){ }
    }
}
