using System;
using Dapper;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlyList : IWriteOnlyPersistentList
    {
        private readonly SqlServerWriteOnlyTransaction _transaction;

        public SqlServerWriteOnlyList(SqlServerWriteOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void AddToLeft(string key, string value)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"insert into HangFire.List ([Key], Value) values (@key, @value)",
                new { key, value }));
        }

        public void Remove(string key, string value)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"delete from HangFire.List where [Key] = @key and Value = @value",
                new { key, value }));
        }

        public void Trim(string key, int keepStartingFrom, int keepEndingAt)
        {
            const string trimSql = @"
with cte as (
select row_number() over (order by Id desc) as row_num from HangFire.List)
delete from cte where row_num not between @start and @end";

            _transaction.QueueCommand(x => x.Execute(
                trimSql,
                new { start = keepStartingFrom + 1, end = keepEndingAt + 1 }));
        }
    }
}