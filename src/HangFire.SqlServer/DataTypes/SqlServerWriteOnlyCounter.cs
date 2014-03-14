using System;
using Dapper;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlyCounter : IWriteOnlyPersistentCounter
    {
        private readonly SqlServerWriteOnlyTransaction _transaction;

        public SqlServerWriteOnlyCounter(SqlServerWriteOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Increment(string key)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value]) values (@key, @value)",
                new { key, value = +1 }));
        }

        public void Increment(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new { key, value = +1, expireAt = DateTime.UtcNow.Add(expireIn) }));
        }

        public void Decrement(string key)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value]) values (@key, @value)",
                new { key, value = -1 }));
        }

        public void Decrement(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new { key, value = -1, expireAt = DateTime.UtcNow.Add(expireIn) }));
        }
    }
}