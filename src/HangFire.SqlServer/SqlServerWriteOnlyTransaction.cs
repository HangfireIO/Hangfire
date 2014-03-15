using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Dapper;
using HangFire.Common;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    internal class SqlServerWriteOnlyTransaction : IWriteOnlyTransaction
    {
        private readonly Queue<Action<SqlConnection>> _commandQueue
            = new Queue<Action<SqlConnection>>();

        private readonly SqlConnection _connection;

        public SqlServerWriteOnlyTransaction(SqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            _connection = connection;
        }

        public void Dispose()
        {
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

        public void ExpireJob(string jobId, TimeSpan expireIn)
        {
            QueueCommand(x => x.Execute(
                @"update HangFire.Job set ExpireAt = @expireAt where Id = @id",
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = jobId }));
        }

        public void PersistJob(string jobId)
        {
            QueueCommand(x => x.Execute(
                @"update HangFire.Job set ExpireAt = NULL where Id = @id",
                new { id = jobId }));
        }

        public void SetJobState(string jobId, string state, IDictionary<string, string> stateProperties)
        {
            QueueCommand(x => x.Execute(
                @"update HangFire.Job set State = @name, StateData = @data where Id = @id",
                new { name = state, data = JobHelper.ToJson(stateProperties), id = jobId }));
        }

        public void AppendJobHistory(string jobId, IDictionary<string, string> properties)
        {
            QueueCommand(x => x.Execute(
                @"insert into HangFire.JobHistory (JobId, CreatedAt, Data) "
                + @"values (@jobId, @createdAt, @data)",
                new { jobId = jobId, createdAt = DateTime.UtcNow, data = JobHelper.ToJson(properties) }));
        }

        public void AddToQueue(string queue, string jobId)
        {
            const string enqueueJobSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@jobId, @queue)";

            QueueCommand(x => x.Execute(
                enqueueJobSql,
                new { jobId = jobId, queue = queue }));
        }

        public void IncrementCounter(string key)
        {
            QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value]) values (@key, @value)",
                new { key, value = +1 }));
        }

        public void IncrementCounter(string key, TimeSpan expireIn)
        {
            QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new { key, value = +1, expireAt = DateTime.UtcNow.Add(expireIn) }));
        }

        public void DecrementCounter(string key)
        {
            QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value]) values (@key, @value)",
                new { key, value = -1 }));
        }

        public void DecrementCounter(string key, TimeSpan expireIn)
        {
            QueueCommand(x => x.Execute(
                @"insert into HangFire.Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new { key, value = -1, expireAt = DateTime.UtcNow.Add(expireIn) }));
        }

        public void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);
        }

        public void AddToSet(string key, string value, double score)
        {
            const string addSql = @"
merge HangFire.[Set] as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);";

            QueueCommand(x => x.Execute(
                addSql,
                new { key, value, score }));
        }

        public void RemoveFromSet(string key, string value)
        {
            QueueCommand(x => x.Execute(
                @"delete from HangFire.[Set] where [Key] = @key and Value = @value",
                new { key, value }));
        }

        public void InsertToList(string key, string value)
        {
            QueueCommand(x => x.Execute(
                @"insert into HangFire.List ([Key], Value) values (@key, @value)",
                new { key, value }));
        }

        public void RemoveFromList(string key, string value)
        {
            QueueCommand(x => x.Execute(
                @"delete from HangFire.List where [Key] = @key and Value = @value",
                new { key, value }));
        }

        public void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            const string trimSql = @"
with cte as (
select row_number() over (order by Id desc) as row_num from HangFire.List)
delete from cte where row_num not between @start and @end";

            QueueCommand(x => x.Execute(
                trimSql,
                new { start = keepStartingFrom + 1, end = keepEndingAt + 1 }));
        }

        public void IncrementValue(string key)
        {
            const string insertSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch";
            const string updateSql = @"
update HangFire.Value set IntValue = IntValue + 1 where [Key] = @key";

            QueueCommand(x =>
            {
                var affectedRows = x.Execute(updateSql, new { key });

                if (affectedRows == 0)
                {
                    x.Execute(insertSql + "\n" + updateSql, new { key });
                }
            });
        }

        public void DecrementValue(string key)
        {
            const string insertSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch";
            const string updateSql = @"
update HangFire.Value set IntValue = IntValue - 1 where [Key] = @key";

            QueueCommand(x =>
            {
                var affectedRows = x.Execute(updateSql, new { key });

                if (affectedRows == 0)
                {
                    x.Execute(insertSql + "\n" + updateSql, new { key });
                }
            });
        }

        public void ExpireValue(string key, TimeSpan expireIn)
        {
            QueueCommand(x => x.Execute(
                @"update HangFire.Value set ExpireAt = @expireAt where [Key] = @key",
                new { expireAt = DateTime.UtcNow.Add(expireIn), key = key }));
        }

        private void QueueCommand(Action<SqlConnection> action)
        {
            _commandQueue.Enqueue(action);
        }
    }
}