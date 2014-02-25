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
    public class SqlStoredJobs : IStoredJobs
    {
        private readonly SqlConnection _connection;

        public SqlStoredJobs(SqlConnection connection)
        {
            _connection = connection;
        }

        public Dictionary<string, string> Get(string id)
        {
            var job = _connection.Query<Job>(
                @"select * from HangFire.Job where id = @id",
                new { id = id })
                .SingleOrDefault();

            if (job == null) return null;

            var data = JobHelper.FromJson<InvocationData>(job.InvocationData);
            return new Dictionary<string, string>
            {
                { "Type", data.Type },
                { "Method" , data.Method },
                { "ParameterTypes", data.ParameterTypes },
                { "Arguments", job.Arguments },
                { "State", job.State },
                { "CreatedAt", JobHelper.ToStringTimestamp(job.CreatedAt) }
            };
        }

        public void SetParameter(string id, string name, string value)
        {
            _connection.Execute(
                @"merge HangFire.JobParameter as Target "
                + @"using (VALUES (@jobId, @name, @value)) as Source (JobId, Name, Value) "
                + @"on Target.JobId = Source.JobId AND Target.Name = Source.Name "
                + @"when matched then update set Value = Source.Value "
                + @"when not matched then insert (JobId, Name, Value) values (Source.JobId, Source.Name, Source.Value)",
                new { jobId = id, name, value });
        }

        public string GetParameter(string id, string name)
        {
            return _connection.Query<string>(
                @"select Value from HangFire.JobParameter where JobId = @id and Name = @name",
                new { id = id, name = name })
                .SingleOrDefault();
        }

        public void Complete(JobPayload payload)
        {
            // TODO: in some cases it is required to not to delete the job.
            _connection.Execute("delete from HangFire.JobQueue where JobId = @id and QueueName = @queueName",
                new { id = payload.Id, queueName = payload.Queue });
        }
    }

    public class SqlStorageConnection : IStorageConnection
    {
        private readonly SqlConnection _connection;

        public SqlStorageConnection(SqlConnection connection)
        {
            _connection = connection;
            Jobs = new SqlStoredJobs(_connection);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IAtomicWriteTransaction CreateWriteTransaction()
        {
            return new SqlWriteTransaction(_connection);
        }

        public IDisposable AcquireJobLock(string jobId)
        {
            return new SqlJobLock(jobId, _connection);
        }

        public IStoredJobs Jobs { get; private set; }

        public void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues)
        {
            var data = new ServerData
            {
                WorkerCount = workerCount,
                Queues = queues.ToArray()
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
    }
}