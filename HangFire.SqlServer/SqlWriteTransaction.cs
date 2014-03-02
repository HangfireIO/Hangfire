using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Dapper;
using HangFire.Common;
using HangFire.SqlServer.Entities;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    public class SqlWriteTransaction : IAtomicWriteTransaction,
        IWriteableJobQueue, IWriteableStoredJobs, IWriteableStoredLists,
        IWriteableStoredSets, IWriteableStoredValues
    {
        private readonly SqlConnection _connection;
        private readonly LinkedList<KeyValuePair<string, object>> _commandList
            = new LinkedList<KeyValuePair<string, object>>();

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

                // I had the idea to put all the command into a single
                // SQL statement, but ran into issue of parameter naming.
                // They should be unique among the whole statement, and 
                // the only way to do it is to generate queries on the fly. 
                // But Dapper documentation states, that it has internal 
                // query cache, that is never flushed. So, query generation 
                // is a bad idea.

                foreach (var command in _commandList)
                {
                    _connection.Execute(command.Key, command.Value);
                }

                transaction.Complete();
            }

            return true;
        }

        void IWriteableJobQueue.Enqueue(string queue, string jobId)
        {
            const string createQueueIfNotExistsSql = @"
begin try
    insert into HangFire.[Queue] (Name) values (@queueName)
end try
begin catch
end catch";

            _commandList.AddLast(new KeyValuePair<string, object>(
                createQueueIfNotExistsSql,
                new { queueName = queue }));

            const string enqueueJobSql = @"
insert into HangFire.JobQueue (JobId, QueueName)
values (@jobId, @queueName)";

            _commandList.AddLast(new KeyValuePair<string, object>(
                enqueueJobSql,
                new { jobId = jobId, queueName = queue }));
        }

        void IWriteableStoredJobs.Create(string jobId, IDictionary<string, string> parameters)
        {
            var data = new InvocationData
            {
                Method = parameters["Method"],
                ParameterTypes = parameters["ParameterTypes"],
                Type = parameters["Type"]
            };

            const string createJobSql = @"
insert into HangFire.Job (Id, State, InvocationData, Arguments, CreatedAt)
values (@id, @state, @invocationData, @arguments, @createdAt)";

            _commandList.AddLast(new KeyValuePair<string, object>(
                createJobSql,
                new
                {
                    id = jobId,
                    state = "Created",
                    invocationData = JobHelper.ToJson(data),
                    arguments = parameters["Arguments"],
                    createdAt = JobHelper.FromStringTimestamp(parameters["CreatedAt"])
                }));
        }

        void IWriteableStoredJobs.Expire(string jobId, TimeSpan expireIn)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"update HangFire.Job set ExpireAt = @expireAt where Id = @id",
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = jobId }));
        }

        void IWriteableStoredJobs.Persist(string jobId)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"update HangFire.Job set ExpireAt = NULL where Id = @id",
                new { id = jobId }));
        }

        void IWriteableStoredJobs.SetState(string jobId, string state, Dictionary<string, string> stateProperties)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"update HangFire.Job set State = @name, StateData = @data where Id = @id",
                new { name = state, data = JobHelper.ToJson(stateProperties), id = jobId }));
        }

        void IWriteableStoredJobs.AppendHistory(string jobId, Dictionary<string, string> properties)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"insert into HangFire.JobHistory (JobId, CreatedAt, Data) "
                + @"values (@jobId, @createdAt, @data)",
                new { jobId = jobId, createdAt = DateTime.UtcNow, data = JobHelper.ToJson(properties) }));
        }

        void IWriteableStoredLists.AddToLeft(string key, string value)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
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

            _commandList.AddLast(new KeyValuePair<string, object>(
                addSql, 
                new { key, value, score }));
        }

        void IWriteableStoredSets.Remove(string key, string value)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"delete from HangFire.[Set] where [Key] = @key and Value = @value",
                new { key, value }));
        }

        void IWriteableStoredLists.Remove(string key, string value)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"delete from HangFire.List where [Key] = @key and Value = @value",
                new { key, value }));
        }

        void IWriteableStoredLists.Trim(string key, int keepStartingFrom, int keepEndingAt)
        {
            const string trimSql = @"
with cte as (
select row_number() over (order by Id desc) as row_num from HangFire.List)
delete from cte where row_num not between @start and @end";

            _commandList.AddLast(new KeyValuePair<string, object>(
                trimSql, 
                new { start = keepStartingFrom + 1, end = keepEndingAt + 1 }));
        }

        void IWriteableStoredValues.Increment(string key)
        {
            const string incrementSql = @"
begin try 
    insert into HangFire.Value ([Key], IntValue) values (@key, 0)
end try
begin catch
end catch

update HangFire.Value with (xlock) set IntValue = IntValue + 1 where [Key] = @key";

            _commandList.AddLast(new KeyValuePair<string, object>(incrementSql, new { key }));
        }

        void IWriteableStoredValues.Decrement(string key)
        {
            const string decrementSql = @"
merge HangFire.Value as Target
using (VALUES (@key)) as Source ([Key])
on Target.[Key] = Source.[Key]
when matched then update set IntValue = IntValue - 1
when not matched then insert ([Key], IntValue) values (Source.[Key], -1);";

            _commandList.AddLast(new KeyValuePair<string, object>(decrementSql, new { key }));
        }

        void IWriteableStoredValues.ExpireIn(string key, TimeSpan expireIn)
        {
            _commandList.AddLast(new KeyValuePair<string, object>(
                @"update HangFire.Value set ExpireAt = @expireAt where [Key] = @key",
                new { expireAt = DateTime.UtcNow.Add(expireIn), key = key }));
        }
    }
}