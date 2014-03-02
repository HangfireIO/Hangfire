using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;
using HangFire.Common;
using HangFire.Server;
using HangFire.SqlServer.Entities;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    public class SqlServerFetcher : IJobFetcher
    {
        private readonly SqlConnection _connection;
        private readonly IEnumerable<string> _queues;

        public SqlServerFetcher(SqlConnection connection, IEnumerable<string> queues)
        {
            _connection = connection;
            _queues = queues;
        }

        public JobPayload DequeueJob(CancellationToken cancellationToken)
        {
            Job job = null;
            string queueName = null;

            do
            {
                // TODO: (FetchedAt < DATEADD(minute, -15, GETUTCDATE()))
                var idAndQueue = _connection.Query(@"
set transaction isolation level read committed
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.JobId, INSERTED.QueueName
where (FetchedAt is null)
and QueueName in @queues",
                    new { queues = _queues })
                    .SingleOrDefault();

                if (idAndQueue != null)
                {
                    // Using DynamicParameters with explicit parameter type 
                    // instead of anonymous object, because of a strange
                    // behaviour of a query plan builder: execution plan
                    // was based on index scan instead of index seek. 
                    // As a result, this query was the slowest.
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", idAndQueue.JobId, dbType: DbType.Guid);

                    job = _connection.Query<Job>(
                        @"select Id, InvocationData, Arguments from HangFire.Job where Id = @id",
                        parameters)
                        .SingleOrDefault();

                    queueName = idAndQueue.QueueName;
                }

                if (job == null)
                {
                    if (cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
                    {
                        return null;
                    }
                }
            } while (job == null);

            var invocationData = JobHelper.FromJson<InvocationData>(job.InvocationData);
            var jobDictionary = new Dictionary<string, string>();

            jobDictionary.Add("Type", invocationData.Type);
            jobDictionary.Add("Method", invocationData.Method);
            jobDictionary.Add("ParameterTypes", invocationData.ParameterTypes);
            jobDictionary.Add("Arguments", job.Arguments);

            return new JobPayload(job.Id.ToString(), queueName, jobDictionary);
        }
    }
}