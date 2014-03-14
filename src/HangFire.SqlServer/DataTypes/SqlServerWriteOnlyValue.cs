using System;
using Dapper;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlyValue : IWriteOnlyPersistentValue
    {
        private readonly SqlServerWriteOnlyTransaction _transaction;

        public SqlServerWriteOnlyValue(SqlServerWriteOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Increment(string key)
        {
            const string insertSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch";
            const string updateSql = @"
update HangFire.Value set IntValue = IntValue + 1 where [Key] = @key";

            _transaction.QueueCommand(x =>
            {
                var affectedRows = x.Execute(updateSql, new { key });

                if (affectedRows == 0)
                {
                    x.Execute(insertSql + "\n" + updateSql, new { key });
                }
            });
        }

        public void Decrement(string key)
        {
            const string insertSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch";
            const string updateSql = @"
update HangFire.Value set IntValue = IntValue - 1 where [Key] = @key";

            _transaction.QueueCommand(x =>
            {
                var affectedRows = x.Execute(updateSql, new { key });

                if (affectedRows == 0)
                {
                    x.Execute(insertSql + "\n" + updateSql, new { key });
                }
            });
        }

        public void ExpireIn(string key, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"update HangFire.Value set ExpireAt = @expireAt where [Key] = @key",
                new { expireAt = DateTime.UtcNow.Add(expireIn), key = key }));
        }
    }
}