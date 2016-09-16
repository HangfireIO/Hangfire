// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
#if NETFULL
using System.Transactions;
#endif
using Dapper;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal class SqlServerWriteOnlyTransaction : JobStorageTransaction
    {
        private readonly Queue<Action<DbConnection, DbTransaction>> _commandQueue
            = new Queue<Action<DbConnection, DbTransaction>>();
        private readonly Queue<Action> _afterCommitCommandQueue = new Queue<Action>(); 

        private readonly SortedSet<string> _lockedResources = new SortedSet<string>();
        private readonly SqlServerStorage _storage;

        public SqlServerWriteOnlyTransaction([NotNull] SqlServerStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            _storage = storage;
        }

        public override void Commit()
        {
            _storage.UseTransaction((connection, transaction) =>
            {
                if (_lockedResources.Count > 0)
                {
                    connection.Execute(
                        "set nocount on;" +
                        "exec sp_getapplock @Resource=@resource, @LockMode=N'Exclusive'",
                        _lockedResources.Select(x => new { resource = x }),
                        transaction);
                }

                foreach (var command in _commandQueue)
                {
                    command(connection, transaction);
                }
            });

            foreach (var command in _afterCommitCommandQueue)
            {
                command();
            }
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            QueueCommand((connection, transaction) => connection.Execute(
                $@"update [{_storage.SchemaName}].Job set ExpireAt = @expireAt where Id = @id",
                new { expireAt = DateTime.UtcNow.Add(expireIn), id = int.Parse(jobId) },
                transaction));
        }

        public override void PersistJob(string jobId)
        {
            QueueCommand((connection, transaction) => connection.Execute(
                $@"update [{_storage.SchemaName}].Job set ExpireAt = NULL where Id = @id",
                new { id = int.Parse(jobId) },
                transaction));
        }

        public override void SetJobState(string jobId, IState state)
        {
            string addAndSetStateSql = 
$@"insert into [{_storage.SchemaName}].State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data);
update [{_storage.SchemaName}].Job set StateId = SCOPE_IDENTITY(), StateName = @name where Id = @id;";

            QueueCommand((connection, transaction) => connection.Execute(
                addAndSetStateSql,
                new
                {
                    jobId = int.Parse(jobId),
                    name = state.Name,
                    reason = state.Reason,
                    createdAt = DateTime.UtcNow,
                    data = JobHelper.ToJson(state.SerializeData()),
                    id = int.Parse(jobId)
                },
                transaction));
        }

        public override void AddJobState(string jobId, IState state)
        {
            string addStateSql =
$@"insert into [{_storage.SchemaName}].State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data)";

            QueueCommand((connection, transaction) => connection.Execute(
                addStateSql,
                new
                {
                    jobId = int.Parse(jobId), 
                    name = state.Name,
                    reason = state.Reason,
                    createdAt = DateTime.UtcNow, 
                    data = JobHelper.ToJson(state.SerializeData())
                },
                transaction));
        }

        public override void AddToQueue(string queue, string jobId)
        {
            var provider = _storage.QueueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue();

            QueueCommand((connection, transaction) => persistentQueue.Enqueue(
                connection,
#if !NETFULL
                transaction,
#endif
                queue,
                jobId));

            if (persistentQueue.GetType() == typeof(SqlServerJobQueue))
            {
                _afterCommitCommandQueue.Enqueue(() => SqlServerJobQueue.NewItemInQueueEvent.Set());
            }
        }

        public override void IncrementCounter(string key)
        {
            QueueCommand((connection, transaction) => connection.Execute(
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value]) values (@key, @value)",
                new { key, value = +1 },
                transaction));
        }

        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            QueueCommand((connection, transaction) => connection.Execute(
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new { key, value = +1, expireAt = DateTime.UtcNow.Add(expireIn) },
                transaction));
        }

        public override void DecrementCounter(string key)
        {
            QueueCommand((connection, transaction) => connection.Execute(
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value]) values (@key, @value)",
                new { key, value = -1 },
                transaction));
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            QueueCommand((connection, transaction) => connection.Execute(
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new { key, value = -1, expireAt = DateTime.UtcNow.Add(expireIn) },
                transaction));
        }

        public override void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);
        }

        public override void AddToSet(string key, string value, double score)
        {
            string addSql =
$@";merge [{_storage.SchemaName}].[Set] with (holdlock) as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);";

            AcquireSetLock();
            QueueCommand((connection, transaction) => connection.Execute(
                addSql,
                new { key, value, score },
                transaction));
        }

        public override void RemoveFromSet(string key, string value)
        {
            string query = $@"delete from [{_storage.SchemaName}].[Set] where [Key] = @key and Value = @value";

            AcquireSetLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query,
                new { key, value },
                transaction));
        }

        public override void InsertToList(string key, string value)
        {
            AcquireListLock();
            QueueCommand((connection, transaction) => connection.Execute(
                $@"insert into [{_storage.SchemaName}].List ([Key], Value) values (@key, @value);",
                new { key, value },
                transaction));
        }

        public override void RemoveFromList(string key, string value)
        {
            AcquireListLock();
            QueueCommand((connection, transaction) => connection.Execute(
                $@"delete from [{_storage.SchemaName}].List where [Key] = @key and Value = @value",
                new { key, value },
                transaction));
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            string trimSql =
$@";with cte as (
    select row_number() over (order by Id desc) as row_num
    from [{_storage.SchemaName}].List
    where [Key] = @key)
delete from cte where row_num not between @start and @end";

            AcquireListLock();
            QueueCommand((connection, transaction) => connection.Execute(
                trimSql,
                new { key = key, start = keepStartingFrom + 1, end = keepEndingAt + 1 },
                transaction));
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            string sql =
$@";merge [{_storage.SchemaName}].Hash with (holdlock) as Target
using (VALUES (@key, @field, @value)) as Source ([Key], Field, Value)
on Target.[Key] = Source.[Key] and Target.Field = Source.Field
when matched then update set Value = Source.Value
when not matched then insert ([Key], Field, Value) values (Source.[Key], Source.Field, Source.Value);";

            AcquireHashLock();
            QueueCommand((connection, transaction) => connection.Execute(
                sql,
                keyValuePairs.Select(y => new { key = key, field = y.Key, value = y.Value }),
                transaction));
        }

        public override void RemoveHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"delete from [{_storage.SchemaName}].Hash where [Key] = @key";

            AcquireHashLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key },
                transaction));
        }

        public override void AddRangeToSet(string key, IList<string> items)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (items == null) throw new ArgumentNullException(nameof(items));

            // TODO: Rewrite using the `MERGE` statement.
            string query =
$@"insert into [{_storage.SchemaName}].[Set] ([Key], Value, Score)
values (@key, @value, 0.0)";

            AcquireSetLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                items.Select(value => new { key = key, value = value }).ToList(),
                transaction));
        }

        public override void RemoveSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"delete from [{_storage.SchemaName}].[Set] where [Key] = @key";

            AcquireSetLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key = key },
                transaction));
        }

        public override void ExpireHash(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

             string query = $@"
update [{_storage.SchemaName}].[Hash] set ExpireAt = @expireAt where [Key] = @key";

            AcquireHashLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key = key, expireAt = DateTime.UtcNow.Add(expireIn) },
                transaction));
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[Set] set ExpireAt = @expireAt where [Key] = @key";

            AcquireSetLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query,
                new { key = key, expireAt = DateTime.UtcNow.Add(expireIn) },
                transaction));
        }

        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[List] set ExpireAt = @expireAt where [Key] = @key";

            AcquireListLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key = key, expireAt = DateTime.UtcNow.Add(expireIn) },
                transaction));
        }

        public override void PersistHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].Hash set ExpireAt = null where [Key] = @key";

            AcquireHashLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key = key },
                transaction));
        }

        public override void PersistSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[Set] set ExpireAt = null where [Key] = @key";

            AcquireSetLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key = key },
                transaction));
        }

        public override void PersistList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[List] set ExpireAt = null where [Key] = @key";

            AcquireListLock();
            QueueCommand((connection, transaction) => connection.Execute(
                query, 
                new { key = key },
                transaction));
        }

        internal void QueueCommand(Action<DbConnection, DbTransaction> action)
        {
            _commandQueue.Enqueue(action);
        }

        private void AcquireListLock()
        {
            AcquireLock("List");
        }

        private void AcquireSetLock()
        {
            AcquireLock("Set");
        }

        private void AcquireHashLock()
        {
            AcquireLock("Hash");
        }

        private void AcquireLock(string resource)
        {
            _lockedResources.Add($"{_storage.SchemaName}:{resource}:Lock");
        }
    }
}