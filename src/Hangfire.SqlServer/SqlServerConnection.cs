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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SqlServer.Entities;
using Hangfire.Storage;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal class SqlServerConnection : JobStorageConnection
    {
        private readonly SqlServerStorage _storage;
        private readonly Dictionary<string, HashSet<Guid>> _lockedResources = new Dictionary<string, HashSet<Guid>>();

        public SqlServerConnection([NotNull] SqlServerStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            _storage = storage;
        }

        public override void Dispose()
        {
            if (_dedicatedConnection != null)
            {
                _dedicatedConnection.Dispose();
                _dedicatedConnection = null;
            }
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new SqlServerWriteOnlyTransaction(_storage, () => _dedicatedConnection);
        }

        public override IDisposable AcquireDistributedLock([NotNull] string resource, TimeSpan timeout)
        {
            if (String.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException(nameof(resource));
            return AcquireLock($"{_storage.SchemaName}:{resource}", timeout);
        }

        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null || queues.Length == 0) throw new ArgumentNullException(nameof(queues));

            var providers = queues
                .Select(queue => _storage.QueueProviders.GetProvider(queue))
                .Distinct()
                .ToArray();

            if (providers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Multiple provider instances registered for queues: {String.Join(", ", queues)}. You should choose only one type of persistent queues per server instance.");
            }
            
            var persistentQueue = providers[0].GetJobQueue();
            return persistentQueue.Dequeue(queues, cancellationToken);
        }

        public override string CreateExpiredJob(
            Job job,
            IDictionary<string, string> parameters, 
            DateTime createdAt,
            TimeSpan expireIn)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var queryString =
$@"insert into [{_storage.SchemaName}].Job (InvocationData, Arguments, CreatedAt, ExpireAt)
output inserted.Id
values (@invocationData, @arguments, @createdAt, @expireAt)";

            var invocationData = InvocationData.SerializeJob(job);
            var payload = invocationData.SerializePayload(excludeArguments: true);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@invocationData", payload, DbType.String, size: -1);
            queryParameters.Add("@arguments", invocationData.Arguments, DbType.String, size: -1);
            queryParameters.Add("@createdAt", createdAt, DbType.DateTime);
            queryParameters.Add("@expireAt", createdAt.Add(expireIn), DbType.DateTime);

            var parametersArray = parameters.ToArray();

            if (parametersArray.Length <= 4)
            {
                if (parametersArray.Length == 1)
                {
                    queryString = $@"
set xact_abort on; set nocount on; declare @jobId bigint;
begin tran;
insert into [{_storage.SchemaName}].Job (InvocationData, Arguments, CreatedAt, ExpireAt) values (@invocationData, @arguments, @createdAt, @expireAt);
select @jobId = scope_identity(); select @jobId;
insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value) values (@jobId, @name, @value);
commit tran;";
                    queryParameters.Add("@name", parametersArray[0].Key, DbType.String, size: 40);
                    queryParameters.Add("@value", parametersArray[0].Value, DbType.String, size: -1);
                }
                else if (parametersArray.Length == 2)
                {
                    queryString = $@"
set xact_abort on; set nocount on; declare @jobId bigint;
begin tran;
insert into [{_storage.SchemaName}].Job (InvocationData, Arguments, CreatedAt, ExpireAt) values (@invocationData, @arguments, @createdAt, @expireAt);
select @jobId = scope_identity(); select @jobId;
insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value) values (@jobId, @name1, @value1), (@jobId, @name2, @value2);
commit tran;";
                    queryParameters.Add("@name1", parametersArray[0].Key, DbType.String, size: 40);
                    queryParameters.Add("@value1", parametersArray[0].Value, DbType.String, size: -1);
                    queryParameters.Add("@name2", parametersArray[1].Key, DbType.String, size: 40);
                    queryParameters.Add("@value2", parametersArray[1].Value, DbType.String, size: -1);
                }
                else if (parametersArray.Length == 3)
                {
                    queryString = $@"
set xact_abort on; set nocount on; declare @jobId bigint;
begin tran;
insert into [{_storage.SchemaName}].Job (InvocationData, Arguments, CreatedAt, ExpireAt) values (@invocationData, @arguments, @createdAt, @expireAt);
select @jobId = scope_identity(); select @jobId;
insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value) values (@jobId, @name1, @value1), (@jobId, @name2, @value2), (@jobId, @name3, @value3);
commit tran;";
                    queryParameters.Add("@name1", parametersArray[0].Key, DbType.String, size: 40);
                    queryParameters.Add("@value1", parametersArray[0].Value, DbType.String, size: -1);
                    queryParameters.Add("@name2", parametersArray[1].Key, DbType.String, size: 40);
                    queryParameters.Add("@value2", parametersArray[1].Value, DbType.String, size: -1);
                    queryParameters.Add("@name3", parametersArray[2].Key, DbType.String, size: 40);
                    queryParameters.Add("@value3", parametersArray[2].Value, DbType.String, size: -1);
                }
                else if (parametersArray.Length == 4)
                {
                    queryString = $@"
set xact_abort on; set nocount on; declare @jobId bigint;
begin tran;
insert into [{_storage.SchemaName}].Job (InvocationData, Arguments, CreatedAt, ExpireAt) values (@invocationData, @arguments, @createdAt, @expireAt);
select @jobId = scope_identity(); select @jobId;
insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value) values (@jobId, @name1, @value1), (@jobId, @name2, @value2), (@jobId, @name3, @value3), (@jobId, @name4, @value4);
commit tran;";
                    queryParameters.Add("@name1", parametersArray[0].Key, DbType.String, size: 40);
                    queryParameters.Add("@value1", parametersArray[0].Value, DbType.String, size: -1);
                    queryParameters.Add("@name2", parametersArray[1].Key, DbType.String, size: 40);
                    queryParameters.Add("@value2", parametersArray[1].Value, DbType.String, size: -1);
                    queryParameters.Add("@name3", parametersArray[2].Key, DbType.String, size: 40);
                    queryParameters.Add("@value3", parametersArray[2].Value, DbType.String, size: -1);
                    queryParameters.Add("@name4", parametersArray[3].Key, DbType.String, size: 40);
                    queryParameters.Add("@value4", parametersArray[3].Value, DbType.String, size: -1);
                }

                return _storage.UseConnection(_dedicatedConnection, connection => connection
                    .ExecuteScalar<long>(queryString, queryParameters, commandTimeout: _storage.CommandTimeout)
                    .ToString());
            }

            return _storage.UseTransaction(_dedicatedConnection, (connection, transaction) =>
            {
                var jobId = connection.ExecuteScalar<long>(
                    queryString,
                    queryParameters,
                    transaction,
                    commandTimeout: _storage.CommandTimeout).ToString();

                var insertParameterSql =
$@"insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value) values (@jobId, @name, @value)";

                using (var commandBatch = new SqlCommandBatch(connection, transaction, preferBatching: _storage.CommandBatchMaxTimeout.HasValue))
                {
                    commandBatch.CommandTimeout = _storage.CommandTimeout;
                    commandBatch.CommandBatchMaxTimeout = _storage.CommandBatchMaxTimeout;

                    foreach (var parameter in parametersArray)
                    {
                        commandBatch.Append(insertParameterSql,
                            new SqlCommandBatchParameter("@jobId", DbType.Int64) { Value = long.Parse(jobId) },
                            new SqlCommandBatchParameter("@name",DbType.String, 40) { Value = parameter.Key },
                            new SqlCommandBatchParameter("@value", DbType.String, -1) { Value = (object)parameter.Value ?? DBNull.Value });
                    }

                    commandBatch.ExecuteNonQuery();
                }

                return jobId;
            }, null);
        }

        public override JobData GetJobData(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            if (!long.TryParse(id, out var parsedId))
            {
                return null;
            }

            string sql =
$@"select InvocationData, StateName, Arguments, CreatedAt from [{_storage.SchemaName}].Job with (readcommittedlock, forceseek) where Id = @id";

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var jobData = connection.Query<SqlJob>(sql, new { id = parsedId }, commandTimeout: _storage.CommandTimeout)
                    .SingleOrDefault();

                if (jobData == null) return null;

                // TODO: conversion exception could be thrown.
                var invocationData = InvocationData.DeserializePayload(jobData.InvocationData);

                if (!String.IsNullOrEmpty(jobData.Arguments))
                {
                    invocationData.Arguments = jobData.Arguments;
                }

                Job job = null;
                JobLoadException loadException = null;

                try
                {
                    job = invocationData.DeserializeJob();
                }
                catch (JobLoadException ex)
                {
                    loadException = ex;
                }

                return new JobData
                {
                    Job = job,
                    State = jobData.StateName,
                    CreatedAt = jobData.CreatedAt,
                    LoadException = loadException
                };
            });
        }

        public override StateData GetStateData(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            if (!long.TryParse(jobId, out var parsedId))
            {
                return null;
            }

            string sql = 
$@"select s.Name, s.Reason, s.Data
from [{_storage.SchemaName}].State s with (readcommittedlock, forceseek)
inner join [{_storage.SchemaName}].Job j with (readcommittedlock, forceseek) on j.StateId = s.Id and j.Id = s.JobId
where j.Id = @jobId";

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var sqlState = connection.Query<SqlState>(sql, new { jobId = parsedId }, commandTimeout: _storage.CommandTimeout).SingleOrDefault();
                if (sqlState == null)
                {
                    return null;
                }

                var data = new Dictionary<string, string>(
                    SerializationHelper.Deserialize<Dictionary<string, string>>(sqlState.Data),
                    StringComparer.OrdinalIgnoreCase);

                return new StateData
                {
                    Name = sqlState.Name,
                    Reason = sqlState.Reason,
                    Data = data
                };
            });
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            // First updated is required for older schema versions (5 and below), where
            // [IX_HangFire_JobParameter_JobIdAndName] index wasn't declared as unique,
            // to make our query resistant to (no matter what schema we are using):
            // 
            // https://github.com/HangfireIO/Hangfire/issues/1743 (deadlocks)
            // https://github.com/HangfireIO/Hangfire/issues/1741 (duplicate entries)
            // https://github.com/HangfireIO/Hangfire/issues/1693#issuecomment-697976133 (records aren't updated)

            var query = $@"
set xact_abort off;
begin try
  update [{_storage.SchemaName}].JobParameter set Value = @value where JobId = @jobId and Name = @name;
  if @@ROWCOUNT = 0 insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value) values (@jobId, @name, @value);
end try
begin catch
  declare @em nvarchar(4000), @es int, @est int;
  select @em=error_message(),@es=error_severity(),@est=error_state();
  IF ERROR_NUMBER() not in (2601, 2627) raiserror(@em, @es, @est);
  update [{_storage.SchemaName}].JobParameter set Value = @value where JobId = @jobId and Name = @name;
end catch";

            _storage.UseConnection(_dedicatedConnection, connection =>
            {
                connection.Execute(
                    query,
                    new { jobId = long.Parse(id), name, value },
                    commandTimeout: _storage.CommandTimeout);
            });
        }

        public override string GetJobParameter(string id, string name)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (!long.TryParse(id, out var parsedId))
            {
                return null;
            }

            return _storage.UseConnection(_dedicatedConnection, connection => connection.ExecuteScalar<string>(
                $@"select top (1) Value from [{_storage.SchemaName}].JobParameter with (readcommittedlock, forceseek) where JobId = @id and Name = @name",
                new { id = parsedId, name = name },
                commandTimeout: _storage.CommandTimeout));
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var result = connection.Query<string>(
                    $@"select Value from [{_storage.SchemaName}].[Set] with (readcommittedlock, forceseek) where [Key] = @key",
                    new { key },
                    commandTimeout: _storage.CommandTimeout);

                return new HashSet<string>(result);
            });
        }

        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            return GetFirstByLowestScoreFromSet(key, fromScore, toScore, 1).FirstOrDefault();
        }

        public override List<string> GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore, int count)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (count <= 0) throw new ArgumentException("The value must be a positive number", nameof(count));
            if (toScore < fromScore) throw new ArgumentException("The `toScore` value must be higher or equal to the `fromScore` value.", nameof(toScore));

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var result = connection.Query<string>(
                    $@"select top (@count) Value from [{_storage.SchemaName}].[Set] with (readcommittedlock, forceseek) where [Key] = @key and Score between @from and @to order by Score",
                    new { count = count, key, from = fromScore, to = toScore },
                    commandTimeout: _storage.CommandTimeout);

                return result.ToList();
            });
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            var sql = $@"
set xact_abort off;
begin try
  insert into [{_storage.SchemaName}].Hash ([Key], Field, Value) values (@key, @field, @value);
  if @@ROWCOUNT = 0 update [{_storage.SchemaName}].Hash set Value = @value where [Key] = @key and Field = @field;
end try
begin catch
  declare @em nvarchar(4000), @es int, @est int;
  select @em=error_message(),@es=error_severity(),@est=error_state();
  IF ERROR_NUMBER() not in (2601, 2627) raiserror(@em, @es, @est);
  update [{_storage.SchemaName}].Hash set Value = @value where [Key] = @key and Field = @field;
end catch";

            var lockResourceKey = $"{_storage.SchemaName}:Hash:Lock";

            _storage.UseTransaction(_dedicatedConnection, (connection, transaction) =>
            {
                using (var commandBatch = new SqlCommandBatch(connection, transaction, preferBatching: _storage.CommandBatchMaxTimeout.HasValue))
                {
                    if (!_storage.Options.DisableGlobalLocks)
                    {
                        commandBatch.Append(
                            "SET XACT_ABORT ON;exec sp_getapplock @Resource=@resource, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=-1;",
                            new SqlCommandBatchParameter("@resource", DbType.String, 255) { Value = lockResourceKey });
                    }

                    foreach (var keyValuePair in keyValuePairs)
                    {
                        commandBatch.Append(sql,
                            new SqlCommandBatchParameter("@key", DbType.String) { Value = key },
                            new SqlCommandBatchParameter("@field", DbType.String, 100) { Value = keyValuePair.Key },
                            new SqlCommandBatchParameter("@value", DbType.String, -1) { Value = (object) keyValuePair.Value ?? DBNull.Value });
                    }

                    if (!_storage.Options.DisableGlobalLocks)
                    {
                        commandBatch.Append(
                            "exec sp_releaseapplock @Resource=@resource, @LockOwner=N'Transaction';",
                            new SqlCommandBatchParameter("@resource", DbType.String, 255) { Value = lockResourceKey });
                    }

                    commandBatch.CommandTimeout = _storage.CommandTimeout;
                    commandBatch.CommandBatchMaxTimeout = _storage.CommandBatchMaxTimeout;

                    commandBatch.ExecuteNonQuery();
                }
            });
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var result = connection.Query<SqlHash>(
                    $"select Field, Value from [{_storage.SchemaName}].Hash with (forceseek, readcommittedlock) where [Key] = @key",
                    new { key },
                    commandTimeout: _storage.CommandTimeout)
                    .ToDictionary(x => x.Field, x => x.Value);

                return result.Count != 0 ? result : null;
            });
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var data = new ServerData
            {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues,
                StartedAt = DateTime.UtcNow,
            };

            _storage.UseConnection(_dedicatedConnection, connection =>
            {
                connection.Execute(
$@";merge [{_storage.SchemaName}].Server with (holdlock) as Target
using (VALUES (@id, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat
when not matched then insert (Id, Data, LastHeartbeat) values (Source.Id, Source.Data, Source.Heartbeat);",
                    new { id = serverId, data = SerializationHelper.Serialize(data), heartbeat = DateTime.UtcNow },
                    commandTimeout: _storage.CommandTimeout);
            });
        }

        public override void RemoveServer(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));

            _storage.UseConnection(_dedicatedConnection, connection =>
            {
                connection.Execute(
                    $@"delete from [{_storage.SchemaName}].Server where Id = @id",
                    new { id = serverId },
                    commandTimeout: _storage.CommandTimeout);
            });
        }

        public override void Heartbeat(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));

            _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var affected = connection.Execute(
                    $@"update [{_storage.SchemaName}].Server set LastHeartbeat = @now where Id = @id",
                    new { now = DateTime.UtcNow, id = serverId },
                    commandTimeout: _storage.CommandTimeout);

                if (affected == 0)
                {
                    throw new BackgroundServerGoneException();
                }
            });
        }

        public override int RemoveTimedOutServers(TimeSpan timeOut)
        {
            if (timeOut.Duration() != timeOut)
            {
                throw new ArgumentException("The `timeOut` value must be positive.", nameof(timeOut));
            }

            return _storage.UseConnection(_dedicatedConnection, connection => connection.Execute(
                $@"delete from [{_storage.SchemaName}].Server where LastHeartbeat < @timeOutAt",
                new { timeOutAt = DateTime.UtcNow.Add(timeOut.Negate()) },
                commandTimeout: _storage.CommandTimeout));
        }

        public override long GetSetCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _storage.UseConnection(_dedicatedConnection, connection => connection.Query<int>(
                $"select count(*) from [{_storage.SchemaName}].[Set] with (readcommittedlock, forceseek) where [Key] = @key",
                new { key = key },
                commandTimeout: _storage.CommandTimeout).First());
        }

        public override List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query =
$@"select [Value] from (
	select [Value], row_number() over (order by [Score] ASC) as row_num
	from [{_storage.SchemaName}].[Set] with (readcommittedlock, forceseek)
	where [Key] = @key 
) as s where s.row_num between @startingFrom and @endingAt";

            return _storage.UseConnection(_dedicatedConnection, connection => connection
                .Query<string>(query, new { key = key, startingFrom = startingFrom + 1, endingAt = endingAt + 1 }, commandTimeout: _storage.CommandTimeout)
                .ToList());
        }

        public override TimeSpan GetSetTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"select min([ExpireAt]) from [{_storage.SchemaName}].[Set] with (readcommittedlock, forceseek) where [Key] = @key";

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var result = connection.ExecuteScalar<DateTime?>(query, new { key = key }, commandTimeout: _storage.CommandTimeout);
                if (!result.HasValue) return TimeSpan.FromSeconds(-1);

                return result.Value - DateTime.UtcNow;
            });
        }

        public override long GetCounter(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = 
$@"select sum(s.[Value]) from (select sum([Value]) as [Value] from [{_storage.SchemaName}].Counter with (readcommittedlock, forceseek)
where [Key] = @key
union all
select [Value] from [{_storage.SchemaName}].AggregatedCounter with (readcommittedlock, forceseek)
where [Key] = @key) as s";

            return _storage.UseConnection(_dedicatedConnection, connection => 
                connection.ExecuteScalar<long?>(query, new { key = key }, commandTimeout: _storage.CommandTimeout) ?? 0);
        }

        public override long GetHashCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"select count(*) from [{_storage.SchemaName}].Hash with (readcommittedlock, forceseek) where [Key] = @key";

            return _storage.UseConnection(_dedicatedConnection, connection => 
                connection.ExecuteScalar<long>(query, new { key = key }, commandTimeout: _storage.CommandTimeout));
        }

        public override TimeSpan GetHashTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"select min([ExpireAt]) from [{_storage.SchemaName}].Hash with (readcommittedlock, forceseek) where [Key] = @key";

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var result = connection.ExecuteScalar<DateTime?>(query, new { key = key }, commandTimeout: _storage.CommandTimeout);
                if (!result.HasValue) return TimeSpan.FromSeconds(-1);

                return result.Value - DateTime.UtcNow;
            });
        }

        public override string GetValueFromHash(string key, string name)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (name == null) throw new ArgumentNullException(nameof(name));

            string query =
$@"select [Value] from [{_storage.SchemaName}].Hash with (readcommittedlock, forceseek)
where [Key] = @key and [Field] = @field";

            return _storage.UseConnection(_dedicatedConnection, connection => connection
                .ExecuteScalar<string>(query, new { key = key, field = name }, commandTimeout: _storage.CommandTimeout));
        }

        public override long GetListCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = 
$@"select count(*) from [{_storage.SchemaName}].List with (readcommittedlock, forceseek)
where [Key] = @key";

            return _storage.UseConnection(_dedicatedConnection, connection => 
                connection.ExecuteScalar<long>(query, new { key = key }, commandTimeout: _storage.CommandTimeout));
        }

        public override TimeSpan GetListTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = 
$@"select min([ExpireAt]) from [{_storage.SchemaName}].List with (readcommittedlock, forceseek)
where [Key] = @key";

            return _storage.UseConnection(_dedicatedConnection, connection =>
            {
                var result = connection.ExecuteScalar<DateTime?>(query, new { key = key }, commandTimeout: _storage.CommandTimeout);
                if (!result.HasValue) return TimeSpan.FromSeconds(-1);

                return result.Value - DateTime.UtcNow;
            });
        }

        public override List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query =
$@"select [Value] from (
	select [Value], row_number() over (order by [Id] desc) as row_num 
	from [{_storage.SchemaName}].List with (readcommittedlock, forceseek)
	where [Key] = @key 
) as s where s.row_num between @startingFrom and @endingAt";

            return _storage.UseConnection(_dedicatedConnection, connection => connection
                .Query<string>(query, new { key = key, startingFrom = startingFrom + 1, endingAt = endingAt + 1 }, commandTimeout: _storage.CommandTimeout)
                .ToList());
        }

        public override List<string> GetAllItemsFromList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query =
$@"select [Value] from [{_storage.SchemaName}].List with (readcommittedlock, forceseek)
where [Key] = @key
order by [Id] desc";

            return _storage.UseConnection(_dedicatedConnection, connection => connection.Query<string>(query, new { key = key }, commandTimeout: _storage.CommandTimeout).ToList());
        }

        private DbConnection _dedicatedConnection;

        private IDisposable AcquireLock(string resource, TimeSpan timeout)
        {
            if (_dedicatedConnection == null)
            {
                _dedicatedConnection = _storage.CreateAndOpenConnection();
            }

            var lockId = Guid.NewGuid();

            if (!_lockedResources.ContainsKey(resource))
            {
                try
                {
                    SqlServerDistributedLock.Acquire(_dedicatedConnection, resource, timeout);
                }
                catch (Exception)
                {
                    ReleaseLock(resource, lockId, true);
                    throw;
                }

                _lockedResources.Add(resource, new HashSet<Guid>());
            }

            _lockedResources[resource].Add(lockId);
            return new DisposableLock(this, resource, lockId);
        }

        private void ReleaseLock(string resource, Guid lockId, bool onDisposing)
        {
            try
            {
                if (_lockedResources.ContainsKey(resource))
                {
                    if (_lockedResources[resource].Contains(lockId))
                    {
                        if (_lockedResources[resource].Remove(lockId) &&
                            _lockedResources[resource].Count == 0 &&
                            _lockedResources.Remove(resource) &&
                            _dedicatedConnection.State == ConnectionState.Open)
                        {
                            // Session-scoped application locks are held only when connection
                            // is open. When connection is closed or broken, for example, when
                            // there was an error, application lock is already released by SQL
                            // Server itself, and we shouldn't do anything.
                            SqlServerDistributedLock.Release(_dedicatedConnection, resource);
                        }
                    }
                }

                if (_lockedResources.Count == 0)
                {
                    _storage.ReleaseConnection(_dedicatedConnection);
                    _dedicatedConnection = null;
                }
            }
            catch (Exception)
            {
                if (!onDisposing)
                {
                    throw;
                }
            }
        }

        private class DisposableLock : IDisposable
        {
            private readonly SqlServerConnection _connection;
            private readonly string _resource;
            private readonly Guid _lockId;

            public DisposableLock(SqlServerConnection connection, string resource, Guid lockId)
            {
                _connection = connection;
                _resource = resource;
                _lockId = lockId;
            }

            public void Dispose()
            {
                _connection.ReleaseLock(_resource, _lockId, true);
            }
        }
    }
}
