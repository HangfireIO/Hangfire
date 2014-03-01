using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Transactions;
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

        public QueuedJob DequeueJob(CancellationToken cancellationToken)
        {
            Job job = null;

            do
            {
                using (var transaction = new TransactionScope(
                    TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var jobId = _connection.Query<Guid?>(@"
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.JobId
where (FetchedAt is null) or (FetchedAt < DATEADD(minute, -15, GETUTCDATE()))
and QueueName in @queues",
                        new { queues = _queues })
                        .SingleOrDefault();

                    if (jobId != null)
                    {
                        job = _connection.Query<Job>(
                            @"select * from HangFire.Job where Id = @id",
                            new { id = jobId })
                            .SingleOrDefault();
                    }

                    transaction.Complete();
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

            return new QueuedJob(new JobPayload(
                job.Id.ToString(), 
                "default", 
                jobDictionary));
        }
    }
}