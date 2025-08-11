// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Data;
using System.Data.Common;
using System.Globalization;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal sealed class SqlServerWriteOnlyTransaction : JobStorageTransaction
    {
        private readonly Queue<Action<DbConnection, DbTransaction>> _queueCommandQueue = new();
        private readonly HashSet<string> _queuesToSignal = new();

        private readonly SqlServerStorage _storage;
        private readonly SqlServerConnection _connection;

        private readonly SortedDictionary<long, List<Func<DbConnection, DbCommand>>> _jobCommands = new();
        private readonly SortedDictionary<string, List<Func<DbConnection, DbCommand>>> _counterCommands = new();
        private readonly SortedDictionary<string, List<Func<DbConnection, DbCommand>>> _hashCommands = new();
        private readonly SortedDictionary<string, List<Func<DbConnection, DbCommand>>> _listCommands = new();
        private readonly SortedDictionary<string, List<Func<DbConnection, DbCommand>>> _setCommands = new();
        private readonly SortedDictionary<string, List<Func<DbConnection, DbCommand>>> _queueCommands = new();
        private readonly List<Tuple<DbCommand, DbParameter, string>> _lockCommands = new();

        private readonly List<SqlServerConnection.DisposableLock> _acquiredLocks = new();

        private readonly SortedSet<string> _lockedResources = new();

        private bool _committed;

        public SqlServerWriteOnlyTransaction([NotNull] SqlServerConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            _connection = connection;
            _storage = connection.Storage;
        }

        public bool Committed => _committed;

        public override void Commit()
        {
            try
            {
                _storage.UseTransaction(_connection.DedicatedConnection, static (storage, connection, transaction, ctx) =>
                {
                    using (var commandBatch = new SqlCommandBatch(connection, transaction, preferBatching: storage.CommandBatchMaxTimeout.HasValue))
                    {
                        commandBatch.Append(connection.Create("set xact_abort on;set nocount on;"));

                        foreach (var lockedResource in ctx._lockedResources)
                        {
                            commandBatch.Append(connection
                                .Create("exec sp_getapplock @Resource=@resource, @LockMode=N'Exclusive'")
                                .AddParameter("@resource", lockedResource, DbType.String, size: 255));
                        }

                        AppendBatch(ctx._jobCommands, commandBatch);
                        AppendBatch(ctx._counterCommands, commandBatch);
                        AppendBatch(ctx._hashCommands, commandBatch);
                        AppendBatch(ctx._listCommands, commandBatch);
                        AppendBatch(ctx._setCommands, commandBatch);
                        AppendBatch(ctx._queueCommands, commandBatch);

                        foreach (var command in ctx._lockCommands)
                        {
                            commandBatch.Append(command.Item1);
                        }

                        commandBatch.CommandTimeout = storage.CommandTimeout;
                        commandBatch.CommandBatchMaxTimeout = storage.CommandBatchMaxTimeout;

                        commandBatch.ExecuteNonQuery();
                        foreach (var acquiredLock in ctx._acquiredLocks)
                        {
                            acquiredLock.TryReportReleased();
                        }

                        foreach (var lockCommand in ctx._lockCommands)
                        {
                            var releaseResult = lockCommand.Item2.GetParameterValue<int?>();
                            if (releaseResult.HasValue && releaseResult.Value < 0)
                            {
                                throw new SqlServerDistributedLockException($"Could not release a lock on the resource '{lockCommand.Item3}': Server returned the '{releaseResult}' error.");
                            }
                        }
                        
                        foreach (var queueCommand in ctx._queueCommandQueue)
                        {
                            queueCommand(connection, transaction);
                        }
                    }
                }, this);

                _committed = true;
            }
            finally
            {
                foreach (var acquiredLock in _acquiredLocks)
                {
                    acquiredLock.Dispose();
                }
            }

            TrySignalListeningWorkers();
        }

        public override void Dispose()
        {
            foreach (var acquiredLock in _acquiredLocks)
            {
                acquiredLock.Dispose();
            }

            base.Dispose();
        }

        public override void AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            if (String.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException(nameof(resource));

            var disposableLock = _connection.AcquireLock($"{_storage.SchemaName}:{resource}", timeout);
            if (disposableLock.OwnLock)
            {
                var command = SqlServerDistributedLock.CreateReleaseCommand(
                    _connection.DedicatedConnection,
                    disposableLock.Resource,
                    out var resultParameter);

                _lockCommands.Add(Tuple.Create(command, resultParameter, disposableLock.Resource));
            }

            _acquiredLocks.Add(disposableLock);
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"update J set ExpireAt = @expireAt from [{schemaName}].Job J with (forceseek) where Id = @id;");

            var longId = long.Parse(jobId, CultureInfo.InvariantCulture);

            AddCommand(_jobCommands, longId, batch => batch.Create(query)
                .AddParameter("@expireAt", DateTime.UtcNow.Add(expireIn), DbType.DateTime)
                .AddParameter("@id", longId, DbType.Int64));
        }

        public override void PersistJob(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"update J set ExpireAt = NULL from [{schemaName}].Job J with (forceseek) where Id = @id;");

            var longId = long.Parse(jobId, CultureInfo.InvariantCulture);

            AddCommand(_jobCommands, longId, batch => batch.Create(query)
                .AddParameter("@id", longId, DbType.Int64));
        }

        public override void SetJobState(string jobId, IState state)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data);
update [{schemaName}].Job set StateId = SCOPE_IDENTITY(), StateName = @name where Id = @jobId;");

            var longId = long.Parse(jobId, CultureInfo.InvariantCulture);

            AddCommand(_jobCommands, longId, batch => batch.Create(query)
                .AddParameter("@jobId", longId, DbType.Int64)
                .AddParameter("@name", state.Name, DbType.String, size: 20)
                .AddParameter("@reason", (object)state.Reason?.Substring(0, Math.Min(99, state.Reason.Length)) ?? DBNull.Value, DbType.String, size: 100)
                .AddParameter("@createdAt", DateTime.UtcNow, DbType.DateTime)
                .AddParameter("@data", (object)SerializationHelper.Serialize(state.SerializeData()) ?? DBNull.Value, DbType.String, size: -1));
        }

        public override void AddJobState(string jobId, IState state)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data)");

            var longId = long.Parse(jobId, CultureInfo.InvariantCulture);

            AddCommand(_jobCommands, longId, batch => batch.Create(query)
                .AddParameter("@jobId", longId, DbType.Int64)
                .AddParameter("@name", state.Name, DbType.String, size: 20)
                .AddParameter("@reason", (object)state.Reason?.Substring(0, Math.Min(99, state.Reason.Length)) ?? DBNull.Value, DbType.String, size: 100)
                .AddParameter("@createdAt", DateTime.UtcNow, DbType.DateTime)
                .AddParameter("@data", (object)SerializationHelper.Serialize(state.SerializeData()) ?? DBNull.Value, DbType.String, size: -1));
        }

        public override void AddToQueue(string queue, string jobId)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            var provider = _storage.QueueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue();

            if (persistentQueue.GetType() == typeof(SqlServerJobQueue))
            {
                var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].JobQueue (JobId, Queue) values (@jobId, @queue)");

                AddCommand(_queueCommands, queue, batch => batch.Create(query)
                    .AddParameter("@jobId", long.Parse(jobId, CultureInfo.InvariantCulture), DbType.Int64)
                    .AddParameter("@queue", queue, DbType.String));

                _queuesToSignal.Add(queue);
            }
            else
            {
#if FEATURE_TRANSACTIONSCOPE
                if (_storage.Options.DisableTransactionScope)
                {
                    throw new NotSupportedException($"`{nameof(SqlServerStorageOptions.DisableTransactionScope)}` option does not support external queue providers");
                }
#endif
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
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].Counter ([Key], [Value]) values (@key, @value)");

            AddCommand(_counterCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value: +1, DbType.Int32));
        }

        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)");

            AddCommand(_counterCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value: +1, DbType.Int32)
                .AddParameter("@expireAt", DateTime.UtcNow.Add(expireIn), DbType.DateTime));
        }

        public override void DecrementCounter(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].Counter ([Key], [Value]) values (@key, @value)");

            AddCommand(_counterCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value: -1, DbType.Int32));
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)");

            AddCommand(_counterCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value: -1, DbType.Int32)
                .AddParameter("@expireAt", DateTime.UtcNow.Add(expireIn), DbType.DateTime));
        }

        public override void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);
        }

        public override void AddToSet(string key, string value, double score)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var query = _storage.GetQueryFromTemplate(_storage.Options.UseIgnoreDupKeyOption
                ? static schemaName => $@"insert into [{schemaName}].[Set] ([Key], Value, Score) values (@key, @value, @score);
if @@ROWCOUNT = 0 update [{schemaName}].[Set] set Score = @score where [Key] = @key and Value = @value;"

                : static schemaName => $@";merge [{schemaName}].[Set] with (xlock) as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);");

            AcquireSetLock(key);
            AddCommand(_setCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value, DbType.String, 256)
                .AddParameter("@score", score, DbType.Double, size: 53));
        }

        public override void RemoveFromSet(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"delete S from [{schemaName}].[Set] S with (forceseek) where [Key] = @key and Value = @value");

            AcquireSetLock(key);
            AddCommand(_setCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value, DbType.String, size: 256));
        }

        public override void InsertToList(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var query = _storage.GetQueryFromTemplate(static schemaName => $@"
select [Key] from [{schemaName}].List with (xlock, forceseek)
where [Key] = @key;
insert into [{schemaName}].List ([Key], Value) values (@key, @value);");

            AcquireListLock(key);
            AddCommand(_listCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value, DbType.String, size: -1));
        }

        public override void RemoveFromList(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"delete L from [{schemaName}].List L with (forceseek) where [Key] = @key and Value = @value");

            AcquireListLock(key);
            AddCommand(_listCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@value", value, DbType.String, size: -1));
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@";with cte as (
    select row_number() over (order by Id desc) as row_num
    from [{schemaName}].List with (xlock, forceseek)
    where [Key] = @key)
delete from cte where row_num not between @start and @end");

            AcquireListLock(key);

            AddCommand(_listCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@start", keepStartingFrom + 1, DbType.Int32)
                .AddParameter("@end", keepEndingAt + 1, DbType.Int32));
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            var query = _storage.GetQueryFromTemplate(_storage.Options.UseIgnoreDupKeyOption
                ? static schemaName => $@"insert into [{schemaName}].Hash ([Key], Field, Value) values (@key, @field, @value);
if @@ROWCOUNT = 0 update [{schemaName}].Hash set Value = @value where [Key] = @key and Field = @field;"

                : static schemaName => $@";merge [{schemaName}].Hash with (xlock) as Target
using (VALUES (@key, @field, @value)) as Source ([Key], Field, Value)
on Target.[Key] = Source.[Key] and Target.Field = Source.Field
when matched then update set Value = Source.Value
when not matched then insert ([Key], Field, Value) values (Source.[Key], Source.Field, Source.Value);");

            AcquireHashLock(key);

            foreach (var pair in keyValuePairs)
            {
                AddCommand(_hashCommands, key, batch => batch.Create(query)
                    .AddParameter("@key", key, DbType.String)
                    .AddParameter("@field", pair.Key, DbType.String, size: 100)
                    .AddParameter("@value", (object)pair.Value ?? DBNull.Value, DbType.String, size: -1));
            }
        }

        public override void RemoveHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"delete H from [{schemaName}].Hash H with (forceseek) where [Key] = @key");

            AcquireHashLock(key);
            AddCommand(_hashCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String));
        }

        public override void AddRangeToSet(string key, IList<string> items)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (items == null) throw new ArgumentNullException(nameof(items));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"insert into [{schemaName}].[Set] ([Key], Value, Score) values (@key, @value, 0.0)");

            AcquireSetLock(key);

            foreach (var item in items)
            {
                AddCommand(_setCommands, key, batch => batch.Create(query)
                    .AddParameter("@key", key, DbType.String)
                    .AddParameter("@value", item, DbType.String, size: 256));
            }
        }

        public override void RemoveSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"delete S from [{schemaName}].[Set] S with (forceseek) where [Key] = @key");

            AcquireSetLock(key);
            AddCommand(_setCommands, key, batch => batch.Create(query).AddParameter("@key", key, DbType.String));
        }

        public override void ExpireHash(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

             var query = _storage.GetQueryFromTemplate(static schemaName => $@"
update [{schemaName}].[Hash] set ExpireAt = @expireAt where [Key] = @key");

            AcquireHashLock(key);
            AddCommand(_hashCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@expireAt", DateTime.UtcNow.Add(expireIn), DbType.DateTime));
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName => $@"
update [{schemaName}].[Set] set ExpireAt = @expireAt where [Key] = @key");

            AcquireSetLock(key);
            AddCommand(_setCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@expireAt", DateTime.UtcNow.Add(expireIn), DbType.DateTime));
        }

        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName => $@"
update [{schemaName}].[List] set ExpireAt = @expireAt where [Key] = @key");

            AcquireListLock(key);
            AddCommand(_listCommands, key, batch => batch.Create(query)
                .AddParameter("@key", key, DbType.String)
                .AddParameter("@expireAt", DateTime.UtcNow.Add(expireIn), DbType.DateTime));
        }

        public override void PersistHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName => $@"
update [{schemaName}].Hash set ExpireAt = null where [Key] = @key");

            AcquireHashLock(key);
            AddCommand(_hashCommands, key, batch => batch.Create(query).AddParameter("@key", key, DbType.String));
        }

        public override void PersistSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName => $@"
update [{schemaName}].[Set] set ExpireAt = null where [Key] = @key");

            AcquireSetLock(key);
            AddCommand(_setCommands, key, batch => batch.Create(query).AddParameter("@key", key, DbType.String));
        }

        public override void PersistList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var query = _storage.GetQueryFromTemplate(static schemaName => $@"
update [{schemaName}].[List] set ExpireAt = null where [Key] = @key");

            AcquireListLock(key);
            AddCommand(_listCommands, key, batch => batch.Create(query).AddParameter("@key", key, DbType.String));
        }

        public override void RemoveFromQueue(IFetchedJob fetchedJob)
        {
            if (fetchedJob == null) throw new ArgumentNullException(nameof(fetchedJob));

            if (fetchedJob is SqlServerTimeoutJob timeoutJob)
            {
                var query = _storage.GetQueryFromTemplate(static schemaName =>
$@"delete JQ from [{schemaName}].JobQueue JQ with (forceseek, rowlock) where Queue = @queue and Id = @id and FetchedAt = @fetchedAt");

                AddCommand(_queueCommands, timeoutJob.Queue, batch => batch.Create(query)
                    .AddParameter("@queue", timeoutJob.Queue, DbType.String)
                    .AddParameter("@id", timeoutJob.Id, DbType.Int64)
                    .AddParameter("@fetchedAt", timeoutJob.FetchedAt, DbType.DateTime));

                timeoutJob.SetTransaction(this);
            }
            else
            {
                throw new NotSupportedException(
                    "Only '" + nameof(SqlServerTimeoutJob) + "' type supports transactional acknowledge, '" + fetchedJob.GetType().Name + "' given.");
            }
        }

        private static void AppendBatch<TKey>(
            SortedDictionary<TKey, List<Func<DbConnection, DbCommand>>> collection,
            SqlCommandBatch batch)
        {
            foreach (var pair in collection)
            {
                foreach (var command in pair.Value)
                {
                    var dbCommand = command(batch.Connection);
                    batch.Append(dbCommand);
                }
            }
        }

        private static void AddCommand<TKey>(
            SortedDictionary<TKey, List<Func<DbConnection, DbCommand>>> collection,
            TKey key,
            Func<DbConnection, DbCommand> command)
        {
            if (!collection.TryGetValue(key, out var commands))
            {
                commands = new List<Func<DbConnection, DbCommand>>();
                collection.Add(key, commands);
            }

            commands.Add(command);
        }

        private void AcquireListLock(string key)
        {
            AcquireLock(_storage.Options.DisableGlobalLocks ? $"List:{key}" : "List");
        }

        private void AcquireSetLock(string key)
        {
            AcquireLock(_storage.Options.DisableGlobalLocks ? $"Set:{key}" : "Set");
        }

        private void AcquireHashLock(string key)
        {
            AcquireLock(_storage.Options.DisableGlobalLocks ? $"Hash:{key}" : "Hash");
        }

        private void AcquireLock(string resource)
        {
            if (!_storage.Options.DisableGlobalLocks || _storage.Options.UseFineGrainedLocks)
            {
                _lockedResources.Add($"{_storage.SchemaName}:{resource}:Lock");
            }
        }

        private void TrySignalListeningWorkers()
        {
            foreach (var queue in _queuesToSignal)
            {
                if (SqlServerJobQueue.NewItemInQueueEvents.TryGetValue(Tuple.Create(_storage, queue), out var signal))
                {
                    signal.Set();
                }
            }
        }
    }
}