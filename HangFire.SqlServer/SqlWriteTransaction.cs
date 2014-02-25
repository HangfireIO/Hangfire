using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
            return true;
        }

        void IWriteableJobQueue.Enqueue(string queue, string jobId)
        {
            _connection.Execute(
                @"merge HangFire.[Queue] as Target "
                + @"using (VALUES (@queue)) as Source (NewQueue) "
                + @"on Target.Name = Source.NewQueue "
                + @"when not matched then "
                + @"insert (Name) Values (NewQueue);",
                new { queue = queue });

            _connection.Execute(
                @"insert into HangFire.JobQueue (JobId, QueueName) "
                + @"values (@jobId, @queueName)",
                new { jobId = jobId, queueName = queue });
        }

        void IWriteableStoredJobs.Create(string jobId, IDictionary<string, string> parameters)
        {
            var data = new InvocationData
            {
                Method = parameters["Method"],
                ParameterTypes = parameters["ParameterTypes"],
                Type = parameters["Type"]
            };

            _connection.Execute(
                @"insert into HangFire.Job (Id, State, InvocationData, Arguments, CreatedAt) "
                + @"values (@id, @state, @invocationData, @arguments, @createdAt)",
                new
                {
                    id = jobId,
                    state = "Created",
                    invocationData = JobHelper.ToJson(data),
                    arguments = parameters["Arguments"],
                    createdAt = JobHelper.FromStringTimestamp(parameters["CreatedAt"])
                });
        }

        void IWriteableStoredJobs.Expire(string jobId, TimeSpan expireIn)
        {
            _connection.Execute(
                @"update HangFire.Job set ExpireAt = @expireAt where Id = @id",
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = jobId });
        }

        void IWriteableStoredJobs.Persist(string jobId)
        {
            _connection.Execute(
                @"update HangFire.Job set ExpireAt = NULL where Id = @id",
                new { id = jobId });
        }

        void IWriteableStoredJobs.SetState(string jobId, string state, Dictionary<string, string> stateProperties)
        {
            _connection.Execute(
                @"update HangFire.Job set State = @name, StateData = @data where Id = @id",
                new { name = state, data = JobHelper.ToJson(stateProperties), id = jobId });
        }

        void IWriteableStoredJobs.AppendHistory(string jobId, Dictionary<string, string> properties)
        {
            _connection.Execute(
                @"insert into HangFire.JobHistory (JobId, CreatedAt, Data) "
                + @"values (@jobId, @createdAt, @data)",
                new { jobId = jobId, createdAt = DateTime.UtcNow, data = JobHelper.ToJson(properties) });
        }

        void IWriteableStoredLists.AddToLeft(string key, string value)
        {
            _connection.Execute(
                @"insert into HangFire.List ([Key], Value) values (@key, @value)",
                new { key, value });
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

            _connection.Execute(addSql, new { key, value, score });
        }

        void IWriteableStoredSets.Remove(string key, string value)
        {
            _connection.Execute(
                @"delete from HangFire.[Set] where [Key] = @key and Value = @value",
                new { key, value });
        }

        void IWriteableStoredLists.Remove(string key, string value)
        {
            _connection.Execute(
                @"delete from HangFire.List where [Key] = @key and Value = @value",
                new { key, value });
        }

        void IWriteableStoredLists.Trim(string key, int keepStartingFrom, int keepEndingAt)
        {
            const string trimSql = @"
with cte as (
select row_number() over (order by Id desc) as row_num from HangFire.List)
delete from cte where row_num not between @start and @end";

            _connection.Execute(trimSql, new { start = keepStartingFrom + 1, end = keepEndingAt + 1 });
        }

        void IWriteableStoredValues.Increment(string key)
        {
            const string decrementSql = @"
merge HangFire.Value as Target
using (VALUES (@key)) as Source ([Key])
on Target.[Key] = Source.[Key]
when matched then update set IntValue = IntValue + 1
when not matched then insert ([Key], IntValue) values (Source.[Key], 1);";

            _connection.Execute(decrementSql, new { key });
        }

        void IWriteableStoredValues.Decrement(string key)
        {
            const string decrementSql = @"
merge HangFire.Value as Target
using (VALUES (@key)) as Source ([Key])
on Target.[Key] = Source.[Key]
when matched then update set IntValue = IntValue - 1
when not matched then insert ([Key], IntValue) values (Source.[Key], -1);";

            _connection.Execute(decrementSql, new { key });
        }

        void IWriteableStoredValues.ExpireIn(string key, TimeSpan expireIn)
        {
            _connection.Execute(
                @"update HangFire.Value set ExpireAt = @expireAt where [Key] = @key",
                new { expireAt = DateTime.UtcNow.Add(expireIn), key = key });
        }
    }
}