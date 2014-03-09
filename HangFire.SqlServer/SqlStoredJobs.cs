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
    internal class SqlStoredJobs : IStoredJobs
    {
        private readonly SqlConnection _connection;

        public SqlStoredJobs(SqlConnection connection)
        {
            _connection = connection;
        }

        public StateAndInvocationData GetStateAndInvocationData(string id)
        {
            var job = _connection.Query<Job>(
                @"select InvocationData, State from HangFire.Job where id = @id",
                new { id = id })
                .SingleOrDefault();

            if (job == null) return null;

            var data = JobHelper.FromJson<InvocationData>(job.InvocationData);

            return new StateAndInvocationData
            {
                InvocationData = data,
                State = job.State,
            };
        }

        public void SetParameter(string id, string name, string value)
        {
            _connection.Execute(
                @"merge HangFire.JobParameter as Target "
                + @"using (VALUES (@jobId, @name, @value)) as Source (JobId, Name, Value) "
                + @"on Target.JobId = Source.JobId AND Target.Name = Source.Name "
                + @"when matched then update set Value = Source.Value "
                + @"when not matched then insert (JobId, Name, Value) values (Source.JobId, Source.Name, Source.Value);",
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
}