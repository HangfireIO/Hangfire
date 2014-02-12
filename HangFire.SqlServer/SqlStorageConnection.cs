using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Dapper;
using HangFire.Common;
using HangFire.SqlServer.Entities;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    public class SqlJobLock : IDisposable
    {
        private readonly TransactionScope _transaction;

        public SqlJobLock(string jobId, IDbConnection connection)
        {
            _transaction = new TransactionScope(
                TransactionScopeOption.Required, 
                new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead });

            connection.Query<string>(
                "select Id from HangFire.Job where Id = @id",
                new { id = jobId });
        }

        public void Dispose()
        {
            _transaction.Complete();
        }
    }

    public class SqlStorageConnection : IStorageConnection
    {
        private readonly SqlConnection _connection;

        public SqlStorageConnection(SqlConnection connection)
        {
            _connection = connection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IAtomicWriteTransaction CreateWriteTransaction()
        {
            throw new NotImplementedException();
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
                @"insert into HangFire.Server (Id, Data) values (@id, @data)",
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
                @"update HangFire.Server set LastHeartbeat = @now",
                new { now = DateTime.UtcNow });
        }
    }
}