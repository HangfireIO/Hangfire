using System;
using System.Collections.Generic;
using Dapper;
using HangFire.Common;
using HangFire.Storage;

namespace HangFire.SqlServer.DataTypes
{
    internal class SqlServerWriteOnlyJob : IWriteOnlyPersistentJob
    {
        private readonly SqlServerWriteOnlyTransaction _transaction;

        public SqlServerWriteOnlyJob(SqlServerWriteOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");

            _transaction = transaction;
        }

        public void Expire(string jobId, TimeSpan expireIn)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"update HangFire.Job set ExpireAt = @expireAt where Id = @id",
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = jobId }));
        }

        public void Persist(string jobId)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"update HangFire.Job set ExpireAt = NULL where Id = @id",
                new { id = jobId }));
        }

        public void SetState(string jobId, string state, Dictionary<string, string> stateProperties)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"update HangFire.Job set State = @name, StateData = @data where Id = @id",
                new { name = state, data = JobHelper.ToJson(stateProperties), id = jobId }));
        }

        public void AppendHistory(string jobId, Dictionary<string, string> properties)
        {
            _transaction.QueueCommand(x => x.Execute(
                @"insert into HangFire.JobHistory (JobId, CreatedAt, Data) "
                + @"values (@jobId, @createdAt, @data)",
                new { jobId = jobId, createdAt = DateTime.UtcNow, data = JobHelper.ToJson(properties) }));
        }
    }
}