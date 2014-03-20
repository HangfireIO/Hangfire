using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using HangFire.Common;
using HangFire.Server;
using HangFire.SqlServer.Entities;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    internal class SqlServerConnection : IStorageConnection
    {
        private readonly SqlConnection _connection;

        public SqlServerConnection(JobStorage storage, SqlConnection connection)
        {
            _connection = connection;
            Storage = storage;
        }

        public JobStorage Storage { get; private set; }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new SqlServerWriteOnlyTransaction(_connection);
        }

        public IJobFetcher CreateFetcher(IEnumerable<string> queueNames)
        {
            return new SqlServerFetcher(_connection, queueNames.ToArray());
        }

        public IDisposable AcquireJobLock(string jobId)
        {
            return new SqlServerDistributedLock(
                String.Format("HangFire:Job:{0}", jobId), 
                _connection);
        }

        public string CreateExpiredJob(
            InvocationData invocationData,
            string[] arguments,
            IDictionary<string, string> parameters, 
            TimeSpan expireIn)
        {
            const string createJobSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt)
values (@invocationData, @arguments, @createdAt, @expireAt);
SELECT CAST(SCOPE_IDENTITY() as int)";

            var jobId = _connection.Query<int>(
                createJobSql,
                new
                {
                    invocationData = JobHelper.ToJson(invocationData),
                    arguments = JobHelper.ToJson(arguments),
                    createdAt = DateTime.UtcNow,
                    expireAt = DateTime.UtcNow.Add(expireIn)
                }).Single().ToString();

            if (parameters.Count > 0)
            {
                var parameterArray = new object[parameters.Count];
                int parameterIndex = 0;
                foreach (var parameter in parameters)
                {
                    parameterArray[parameterIndex++] = new
                    {
                        jobId = jobId,
                        name = parameter.Key,
                        value = parameter.Value
                    };
                }

                const string insertParameterSql = @"
insert into HangFire.JobParameter (JobId, Name, Value)
values (@jobId, @name, @value)";

                _connection.Execute(insertParameterSql, parameterArray);
            }

            return jobId;
        }

        public StateAndInvocationData GetJobStateAndInvocationData(string id)
        {
            const string sql = @"
select InvocationData, StateName 
from HangFire.Job
where id = @id";

            var job = _connection.Query<SqlJob>(sql, new { id = id })
                .SingleOrDefault();

            if (job == null) return null;

            var data = JobHelper.FromJson<InvocationData>(job.InvocationData);

            return new StateAndInvocationData
            {
                InvocationData = data,
                State = job.StateName,
            };
        }

        public void SetJobParameter(string id, string name, string value)
        {
            _connection.Execute(
                @"merge HangFire.JobParameter as Target "
                + @"using (VALUES (@jobId, @name, @value)) as Source (JobId, Name, Value) "
                + @"on Target.JobId = Source.JobId AND Target.Name = Source.Name "
                + @"when matched then update set Value = Source.Value "
                + @"when not matched then insert (JobId, Name, Value) values (Source.JobId, Source.Name, Source.Value);",
                new { jobId = id, name, value });
        }

        public string GetJobParameter(string id, string name)
        {
            return _connection.Query<string>(
                @"select Value from HangFire.JobParameter where JobId = @id and Name = @name",
                new { id = id, name = name })
                .SingleOrDefault();
        }

        public void CompleteJob(JobPayload payload)
        {
            _connection.Execute("delete from HangFire.JobQueue where JobId = @id and Queue = @queueName",
                new { id = payload.Id, queueName = payload.Queue });
        }

        public string GetFirstByLowestScoreFromSet(string key, long fromScore, long toScore)
        {
            return _connection.Query<string>(
                @"select top 1 Value from HangFire.[Set] where [Key] = @key and Score between @from and @to order by Score",
                new { key, from = fromScore, to = toScore })
                .SingleOrDefault();
        }

        public void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues)
        {
            var data = new ServerData
            {
                WorkerCount = workerCount,
                Queues = queues.ToArray(),
                StartedAt = DateTime.UtcNow,
            };

            _connection.Execute(
                @"merge HangFire.Server as Target "
                + @"using (VALUES (@id, @data)) as Source (Id, Data) "
                + @"on Target.Id = Source.Id "
                + @"when matched then update set Data = Source.Data, LastHeartbeat = null "
                + @"when not matched then insert (Id, Data) values (Source.Id, Source.Data);",
                new { id = serverId, data = JobHelper.ToJson(data) });
        }

        public void RemoveServer(string serverId)
        {
            _connection.Execute(
                @"delete from HangFire.Server where Id = @id",
                new { id = serverId });
        }

        public void Heartbeat(string serverId)
        {
            _connection.Execute(
                @"update HangFire.Server set LastHeartbeat = @now where Id = @id",
                new { now = DateTime.UtcNow, id = serverId });
        }

        public int RemoveTimedOutServers(TimeSpan timeOut)
        {
            return _connection.Execute(
                @"delete from HangFire.Server where LastHeartbeat < @timeOutAt",
                new { timeOutAt = DateTime.UtcNow.Add(timeOut.Negate()) });
        }
    }
}