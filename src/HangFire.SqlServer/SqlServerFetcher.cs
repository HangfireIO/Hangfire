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
    internal class SqlServerFetcher : IJobFetcher
    {
        private static readonly TimeSpan JobTimeOut = TimeSpan.FromMinutes(30);

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

            const string fetchJobSql = @"
set transaction isolation level read committed
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.JobId, INSERTED.Queue
where FetchedAt is null
and Queue in @queues";

            const string fetchTimedOutJobSql = @"
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.JobId, INSERTED.Queue
where FetchedAt < DATEADD(second, @timeout, GETUTCDATE())
and Queue in @queues";

            var fetchQueries = new[] { fetchJobSql, fetchTimedOutJobSql };
            var currentQueryIndex = 0;

            do
            {
                var idAndQueue = _connection.Query(
                    fetchQueries[currentQueryIndex],
                    new { queues = _queues, timeout = JobTimeOut.Negate().TotalSeconds })
                    .SingleOrDefault();

                if (idAndQueue != null)
                {
                    // Using DynamicParameters with explicit parameter type 
                    // instead of anonymous object, because of a strange
                    // behaviour of a query plan builder: execution plan
                    // was based on index scan instead of index seek. 
                    // As a result, this query was the slowest.
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", idAndQueue.JobId, dbType: DbType.Int32);

                    job = _connection.Query<Job>(
                        @"select Id, InvocationData, Arguments from HangFire.Job where Id = @id",
                        parameters)
                        .SingleOrDefault();

                    queueName = idAndQueue.Queue;
                }

                if (job == null && currentQueryIndex == fetchQueries.Length - 1)
                {
                    if (cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
                    {
                        return null;
                    }
                }

                currentQueryIndex = (currentQueryIndex + 1) % fetchQueries.Length;
            } while (job == null);

            var invocationData = JobHelper.FromJson<InvocationData>(job.InvocationData);

            return new JobPayload(job.Id.ToString(), queueName, invocationData)
            {
                Arguments = job.Arguments
            };
        }
    }
}