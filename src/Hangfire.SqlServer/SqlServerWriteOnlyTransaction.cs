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
using System.Data.SqlClient;
#if FEATURE_TRANSACTIONSCOPE
using System.Transactions;
#endif
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal class SqlServerWriteOnlyTransaction : JobStorageTransaction
    {
        private readonly Queue<Action<DbConnection, DbTransaction>> _queueCommandQueue
            = new Queue<Action<DbConnection, DbTransaction>>();
        private readonly Queue<Action> _afterCommitCommandQueue = new Queue<Action>();

        private readonly SqlServerStorage _storage;
        private readonly Func<DbConnection> _dedicatedConnectionFunc;

        private readonly SortedDictionary<long, List<Tuple<string, SqlParameter[]>>> _jobCommands = new SortedDictionary<long, List<Tuple<string, SqlParameter[]>>>();
        private readonly SortedDictionary<string, List<Tuple<string, SqlParameter[]>>> _counterCommands = new SortedDictionary<string, List<Tuple<string, SqlParameter[]>>>();
        private readonly SortedDictionary<string, List<Tuple<string, SqlParameter[]>>> _hashCommands = new SortedDictionary<string, List<Tuple<string, SqlParameter[]>>>();
        private readonly SortedDictionary<string, List<Tuple<string, SqlParameter[]>>> _listCommands = new SortedDictionary<string, List<Tuple<string, SqlParameter[]>>>();
        private readonly SortedDictionary<string, List<Tuple<string, SqlParameter[]>>> _setCommands = new SortedDictionary<string, List<Tuple<string, SqlParameter[]>>>();
        private readonly SortedDictionary<string, List<Tuple<string, SqlParameter[]>>> _queueCommands = new SortedDictionary<string, List<Tuple<string, SqlParameter[]>>>();

        public SqlServerWriteOnlyTransaction([NotNull] SqlServerStorage storage, Func<DbConnection> dedicatedConnectionFunc)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            _storage = storage;
            _dedicatedConnectionFunc = dedicatedConnectionFunc;
        }

        public override void Commit()
        {
            _storage.UseTransaction(_dedicatedConnectionFunc(), (connection, transaction) =>
            {
                using (var commandBatch = new SqlCommandBatch(preferBatching: _storage.CommandBatchMaxTimeout.HasValue))
                {
                    commandBatch.Append("set xact_abort on;set nocount on;");

                    AppendBatch(_jobCommands, commandBatch);
                    AppendBatch(_counterCommands, commandBatch);
                    AppendBatch(_hashCommands, commandBatch);
                    AppendBatch(_listCommands, commandBatch);
                    AppendBatch(_setCommands, commandBatch);
                    AppendBatch(_queueCommands, commandBatch);

                    commandBatch.Connection = connection;
                    commandBatch.Transaction = transaction;
                    commandBatch.CommandTimeout = _storage.CommandTimeout;
                    commandBatch.CommandBatchMaxTimeout = _storage.CommandBatchMaxTimeout;

                    commandBatch.ExecuteNonQuery();

                    foreach (var queueCommand in _queueCommandQueue)
                    {
                        queueCommand(connection, transaction);
                    }
                }
            });

            foreach (var command in _afterCommitCommandQueue)
            {
                command();
            }
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            AddCommand(
                _jobCommands,
                long.Parse(jobId),
                $@"update J set ExpireAt = @expireAt from [{_storage.SchemaName}].Job J with (forceseek) where Id = @id;",
                new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)),
                new SqlParameter("@id", long.Parse(jobId)));
        }

        public override void PersistJob(string jobId)
        {
            AddCommand(
                _jobCommands,
                long.Parse(jobId),
                $@"update J set ExpireAt = NULL from [{_storage.SchemaName}].Job J with (forceseek) where Id = @id;",
                new SqlParameter("@id", long.Parse(jobId)));
        }

        public override void SetJobState(string jobId, IState state)
        {
            string addAndSetStateSql = 
$@"insert into [{_storage.SchemaName}].State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data);
update [{_storage.SchemaName}].Job set StateId = SCOPE_IDENTITY(), StateName = @name where Id = @id;";

            AddCommand(
                _jobCommands,
                long.Parse(jobId),
                addAndSetStateSql,
                new SqlParameter("@jobId", long.Parse(jobId)),
                new SqlParameter("@name", state.Name),
                new SqlParameter("@reason", (object)state.Reason?.Substring(0, Math.Min(99, state.Reason.Length)) ?? DBNull.Value),
                new SqlParameter("@createdAt", DateTime.UtcNow),
                new SqlParameter("@data", (object)JobHelper.ToJson(state.SerializeData()) ?? DBNull.Value),
                new SqlParameter("@id", long.Parse(jobId)));
        }

        public override void AddJobState(string jobId, IState state)
        {
            string addStateSql =
$@"insert into [{_storage.SchemaName}].State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data)";

            AddCommand(
                _jobCommands,
                long.Parse(jobId),
                addStateSql,
                new SqlParameter("@jobId", long.Parse(jobId)),
                new SqlParameter("@name", state.Name),
                new SqlParameter("@reason", (object)state.Reason?.Substring(0, Math.Min(99, state.Reason.Length)) ?? DBNull.Value),
                new SqlParameter("@createdAt", DateTime.UtcNow),
                new SqlParameter("@data", (object)JobHelper.ToJson(state.SerializeData()) ?? DBNull.Value));
        }

        public override void AddToQueue(string queue, string jobId)
        {
            var provider = _storage.QueueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue();

            if (persistentQueue.GetType() == typeof(SqlServerJobQueue))
            {
                AddCommand(
                    _queueCommands,
                    queue,
                    $@"insert into [{_storage.SchemaName}].JobQueue (JobId, Queue) values (@jobId, @queue)",
                    new SqlParameter("@jobId", jobId),
                    new SqlParameter("@queue", queue));

                _afterCommitCommandQueue.Enqueue(() => SqlServerJobQueue.NewItemInQueueEvent.Set());
            }
            else
            {
                _queueCommandQueue.Enqueue((connection, transaction) => persistentQueue.Enqueue(
                    connection,
#if !FEATURE_TRANSACTIONSCOPE
                    transaction,
#endif
                    queue,
                    jobId));
            }
        }

        public override void IncrementCounter(string key)
        {
            AddCommand(
                _counterCommands,
                key,
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value]) values (@key, @value)",
                new SqlParameter("@key", key),
                new SqlParameter("@value", +1));
        }

        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            AddCommand(
                _counterCommands,
                key,
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new SqlParameter("@key", key),
                new SqlParameter("@value", +1),
                new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
        }

        public override void DecrementCounter(string key)
        {
            AddCommand(
                _counterCommands,
                key,
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value]) values (@key, @value)",
                new SqlParameter("@key", key),
                new SqlParameter("@value", -1));
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            AddCommand(
                _counterCommands,
                key,
                $@"insert into [{_storage.SchemaName}].Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)",
                new SqlParameter("@key", key),
                new SqlParameter("@value", -1),
                new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
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

            AddCommand(
                _setCommands,
                key,
                addSql,
                new SqlParameter("@key", key),
                new SqlParameter("@value", value),
                new SqlParameter("@score", score));
        }

        public override void RemoveFromSet(string key, string value)
        {
            string query = $@"delete from [{_storage.SchemaName}].[Set] where [Key] = @key and Value = @value";

            AddCommand(
                _setCommands,
                key,
                query,
                new SqlParameter("@key", key),
                new SqlParameter("@value", value));
        }

        public override void InsertToList(string key, string value)
        {
            AddCommand(
                _listCommands,
                key,
                $@"
select [Key] from [{_storage.SchemaName}].List with (xlock)
where [Key] = @key;
insert into [{_storage.SchemaName}].List ([Key], Value) values (@key, @value);",
                new SqlParameter("@key", key),
                new SqlParameter("@value", value));
        }

        public override void RemoveFromList(string key, string value)
        {
            AddCommand(
                _listCommands,
                key,
                $@"delete from [{_storage.SchemaName}].List where [Key] = @key and Value = @value",
                new SqlParameter("@key", key),
                new SqlParameter("@value", value));
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            string trimSql =
$@";with cte as (
    select row_number() over (order by Id desc) as row_num
    from [{_storage.SchemaName}].List with (xlock)
    where [Key] = @key)
delete from cte where row_num not between @start and @end";

            AddCommand(
                _listCommands,
                key, 
                trimSql,
                new SqlParameter("@key", key),
                new SqlParameter("@start", keepStartingFrom + 1),
                new SqlParameter("@end", keepEndingAt + 1));
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

            foreach (var pair in keyValuePairs)
            {
                AddCommand(
                    _hashCommands,
                    key,
                    sql,
                    new SqlParameter("@key", key),
                    new SqlParameter("@field", pair.Key),
                    new SqlParameter("@value", (object)pair.Value ?? DBNull.Value));
            }
        }

        public override void RemoveHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"delete from [{_storage.SchemaName}].Hash where [Key] = @key";

            AddCommand(_hashCommands, key, query, new SqlParameter("@key", key));
        }

        public override void AddRangeToSet(string key, IList<string> items)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (items == null) throw new ArgumentNullException(nameof(items));

            // TODO: Rewrite using the `MERGE` statement.
            string query =
$@"insert into [{_storage.SchemaName}].[Set] ([Key], Value, Score)
values (@key, @value, 0.0)";

            foreach (var item in items)
            {
                AddCommand(_setCommands, key, query, new SqlParameter("@key", key), new SqlParameter("@value", item));
            }
        }

        public override void RemoveSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"delete from [{_storage.SchemaName}].[Set] where [Key] = @key";
            AddCommand(_setCommands, key, query, new SqlParameter("@key", key));
        }

        public override void ExpireHash(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

             string query = $@"
update [{_storage.SchemaName}].[Hash] set ExpireAt = @expireAt where [Key] = @key";

            AddCommand(_hashCommands, key, query,
                new SqlParameter("@key", key),
                new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[Set] set ExpireAt = @expireAt where [Key] = @key";

            AddCommand(_setCommands, key, query,
                new SqlParameter("@key", key),
                new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
        }

        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[List] set ExpireAt = @expireAt where [Key] = @key";

            AddCommand(_listCommands, key, query,
                new SqlParameter("@key", key),
                new SqlParameter("@expireAt", DateTime.UtcNow.Add(expireIn)));
        }

        public override void PersistHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].Hash set ExpireAt = null where [Key] = @key";

            AddCommand(_hashCommands, key, query, new SqlParameter("@key", key));
        }

        public override void PersistSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[Set] set ExpireAt = null where [Key] = @key";

            AddCommand(_setCommands, key, query, new SqlParameter("@key", key));
        }

        public override void PersistList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"
update [{_storage.SchemaName}].[List] set ExpireAt = null where [Key] = @key";

            AddCommand(_listCommands, key, query, new SqlParameter("@key", key));
        }

        private void AppendBatch<TKey>(
            SortedDictionary<TKey, List<Tuple<string, SqlParameter[]>>> collection,
            SqlCommandBatch batch)
        {
            foreach (var pair in collection)
            {
                foreach (var command in pair.Value)
                {
                    batch.Append(command.Item1, command.Item2);
                }
            }
        }

        private void AddCommand<TKey>(
            SortedDictionary<TKey, List<Tuple<string, SqlParameter[]>>> collection,
            TKey key, 
            string commandText, 
            params SqlParameter[] parameters)
        {
            List<Tuple<string, SqlParameter[]>> commands;

            if (!collection.TryGetValue(key, out commands))
            {
                commands = new List<Tuple<string, SqlParameter[]>>();
                collection.Add(key, commands);
            }

            commands.Add(Tuple.Create(commandText, parameters));
        }
    }
}