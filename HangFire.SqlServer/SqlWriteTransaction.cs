using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Dapper;
using HangFire.Common;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    public class SqlWriteTransaction : IAtomicWriteTransaction,
        IWriteableJobQueue, IWriteableStoredJobs, IWriteableStoredLists,
        IWriteableStoredSets, IWriteableStoredValues
    {
        private readonly Queue<Action<SqlConnection>> _commandQueue
            = new Queue<Action<SqlConnection>>();

        private readonly SqlConnection _connection;

        public SqlWriteTransaction(SqlConnection connection)
        {
            _connection = connection;
        }

        public void Dispose()
        {
        }

        public IWriteableStoredValues Values { get { return this; } }
        public IWriteableStoredSets Sets { get { return this; } }
        public IWriteableStoredLists Lists { get { return this; } }
        public IWriteableJobQueue Queues { get { return this; } }
        public IWriteableStoredJobs Jobs { get { return this; } }

        public bool Commit()
        {
            using (var transaction = new TransactionScope(
                TransactionScopeOption.RequiresNew,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
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

        void IWriteableJobQueue.Enqueue(string queue, string jobId)
        {
            const string enqueueJobSql = @"
insert into HangFire.JobQueue (JobId, QueueName)
values (@jobId, @queueName)";

            _commandQueue.Enqueue(x => x.Execute(
                enqueueJobSql,
                new { jobId = jobId, queueName = queue }));
        }

        void IWriteableStoredJobs.Expire(string jobId, TimeSpan expireIn)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"update HangFire.Job set ExpireAt = @expireAt where Id = @id",
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = jobId }));
        }

        void IWriteableStoredJobs.Persist(string jobId)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"update HangFire.Job set ExpireAt = NULL where Id = @id",
                new { id = jobId }));
        }

        void IWriteableStoredJobs.SetState(string jobId, string state, Dictionary<string, string> stateProperties)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"update HangFire.Job set State = @name, StateData = @data where Id = @id",
                new { name = state, data = JobHelper.ToJson(stateProperties), id = jobId }));
        }

        void IWriteableStoredJobs.AppendHistory(string jobId, Dictionary<string, string> properties)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"insert into HangFire.JobHistory (JobId, CreatedAt, Data) "
                + @"values (@jobId, @createdAt, @data)",
                new { jobId = jobId, createdAt = DateTime.UtcNow, data = JobHelper.ToJson(properties) }));
        }

        void IWriteableStoredLists.AddToLeft(string key, string value)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"insert into HangFire.List ([Key], Value) values (@key, @value)",
                new { key, value }));
        }

        void IWriteableStoredSets.Add(string key, string value)
        {
            ((IWriteableStoredSets)this).Add(key, value, 0.0);
        }

        void IWriteableStoredSets.Add(string key, string value, double score)
        {
            const string addSql = @"
merge HangFire.[Set] as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);";

            _commandQueue.Enqueue(x => x.Execute(
                addSql, 
                new { key, value, score }));
        }

        void IWriteableStoredSets.Remove(string key, string value)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"delete from HangFire.[Set] where [Key] = @key and Value = @value",
                new { key, value }));
        }

        void IWriteableStoredLists.Remove(string key, string value)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"delete from HangFire.List where [Key] = @key and Value = @value",
                new { key, value }));
        }

        void IWriteableStoredLists.Trim(string key, int keepStartingFrom, int keepEndingAt)
        {
            const string trimSql = @"
with cte as (
select row_number() over (order by Id desc) as row_num from HangFire.List)
delete from cte where row_num not between @start and @end";

            _commandQueue.Enqueue(x => x.Execute(
                trimSql, 
                new { start = keepStartingFrom + 1, end = keepEndingAt + 1 }));
        }

        void IWriteableStoredValues.Increment(string key)
        {
            const string insertSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch";
            const string updateSql = @"
update HangFire.Value set IntValue = IntValue + 1 where [Key] = @key";

            _commandQueue.Enqueue(x =>
            {
                var affectedRows = x.Execute(updateSql, new { key });

                if (affectedRows == 0)
                {
                    x.Execute(insertSql + "\n" + updateSql, new { key });
                }
            });
        }

        void IWriteableStoredValues.Decrement(string key)
        {
            const string insertSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch";
            const string updateSql = @"
update HangFire.Value set IntValue = IntValue - 1 where [Key] = @key";

            _commandQueue.Enqueue(x =>
            {
                var affectedRows = x.Execute(updateSql, new { key });

                if (affectedRows == 0)
                {
                    x.Execute(insertSql + "\n" + updateSql, new { key });
                }
            });
        }

        void IWriteableStoredValues.ExpireIn(string key, TimeSpan expireIn)
        {
            _commandQueue.Enqueue(x => x.Execute(
                @"update HangFire.Value set ExpireAt = @expireAt where [Key] = @key",
                new { expireAt = DateTime.UtcNow.Add(expireIn), key = key }));
        }
    }
}