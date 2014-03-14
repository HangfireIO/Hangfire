using System;
using Dapper;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlyQueue : IWriteOnlyPersistentQueue
    {
        private readonly SqlServerWriteOnlyTransaction _transaction;

        public SqlServerWriteOnlyQueue(SqlServerWriteOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Enqueue(string queue, string jobId)
        {
            const string enqueueJobSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@jobId, @queue)";

            _transaction.QueueCommand(x => x.Execute(
                enqueueJobSql,
                new { jobId = jobId, queue = queue }));
        }
    }
}