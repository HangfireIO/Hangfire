using System;
using System.Data;
using System.Transactions;
using Dapper;

namespace HangFire.SqlServer
{
    public class SqlJobLock : IDisposable
    {
        private readonly TransactionScope _transaction;
        private bool _completed;

        public SqlJobLock(string jobId, IDbConnection connection)
        {
            _transaction = new TransactionScope(
                TransactionScopeOption.Required, 
                new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead });

            connection.Query<Guid>(
                "select Id from HangFire.Job where Id = @id",
                new { id = jobId });
        }

        public void Dispose()
        {
            if (!_completed)
            {
                _completed = true;
                _transaction.Complete();
            }
            _transaction.Dispose();
        }
    }
}