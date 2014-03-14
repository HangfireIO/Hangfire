using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlyTransaction : IWriteOnlyTransaction
    {
        private readonly Queue<Action<SqlConnection>> _commandQueue
            = new Queue<Action<SqlConnection>>();

        private readonly SqlConnection _connection;

        public SqlServerWriteOnlyTransaction(SqlConnection connection)
        {
            _connection = connection;

            Lists = new SqlServerWriteOnlyList(this);
            Queues = new SqlServerWriteOnlyQueue(this);
            Jobs = new SqlServerWriteOnlyJob(this);
            Sets = new SqlServerWriteOnlySet(this);
            Values = new SqlServerWriteOnlyValue(this);
            Counters = new SqlServerWriteOnlyCounter(this);
        }

        public void Dispose()
        {
        }

        public IWriteOnlyPersistentValue Values { get; private set; }
        public IWriteOnlyPersistentSet Sets { get; private set; }
        public IWriteOnlyPersistentList Lists { get; private set; }
        public IWriteOnlyPersistentQueue Queues { get; private set; }
        public IWriteOnlyPersistentJob Jobs { get; private set; }
        public IWriteOnlyPersistentCounter Counters { get; private set; }

        public void QueueCommand(Action<SqlConnection> action)
        {
            _commandQueue.Enqueue(action);
        }

        public bool Commit()
        {
            using (var transaction = new TransactionScope(
                TransactionScopeOption.RequiresNew,
                new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            {
                _connection.EnlistTransaction(Transaction.Current);

                foreach (var command in _commandQueue)
                {
                    command(_connection);
                }

                transaction.Complete();
            }

            return true;
        }
    }
}