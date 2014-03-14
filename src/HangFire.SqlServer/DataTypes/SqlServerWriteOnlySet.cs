using System;
using Dapper;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlySet : IWriteOnlyPersistentSet
    {
        private readonly SqlServerWriteOnlyTransaction _transaction;

        public SqlServerWriteOnlySet(SqlServerWriteOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Add(string key, string value)
        {
            ((IWriteOnlyPersistentSet)this).Add(key, value, 0.0);
        }

        public void Add(string key, string value, double score)
        {
            const string addSql = @"
merge HangFire.[Set] as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);";

            _transaction.QueueCommand(x => x.Execute(
                addSql,
                new { key, value, score }));
        }

        void IWriteOnlyPersistentSet.Remove(string key, string value)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"delete from HangFire.[Set] where [Key] = @key and Value = @value",
                new { key, value }));
        }
    }
}