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

        public SqlServerConnection([NotNull] SqlServerStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            _storage = storage;
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new SqlServerWriteOnlyTransaction(_storage);
        }

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            return new SqlServerDistributedLock(_storage, $"{_storage.SchemaName}:{resource}", timeout);
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

            string createJobSql =
$@"insert into [{_storage.SchemaName}].Job (InvocationData, Arguments, CreatedAt, ExpireAt)
output inserted.Id
values (@invocationData, @arguments, @createdAt, @expireAt)";

            var invocationData = InvocationData.Serialize(job);

            return _storage.UseConnection(connection =>
            {
                var jobId = connection.ExecuteScalar<long>(
                    createJobSql,
                    new
                    {
                        invocationData = JobHelper.ToJson(invocationData),
                        arguments = invocationData.Arguments,
                        createdAt = createdAt,
                        expireAt = createdAt.Add(expireIn)
                    },
                    commandTimeout: _storage.CommandTimeout).ToString();

                if (parameters.Count > 0)
                {
                    var parameterArray = new object[parameters.Count];
                    int parameterIndex = 0;
                    foreach (var parameter in parameters)
                    {
                        parameterArray[parameterIndex++] = new
                        {
                            jobId = long.Parse(jobId),
                            name = parameter.Key,
                            value = parameter.Value
                        };
                    }

                    string insertParameterSql =
$@"insert into [{_storage.SchemaName}].JobParameter (JobId, Name, Value)
values (@jobId, @name, @value)";

                    connection.Execute(insertParameterSql, parameterArray, commandTimeout: _storage.CommandTimeout);
                }

                return jobId;
            });
        }

        public override JobData GetJobData(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            string sql =
$@"select InvocationData, StateName, Arguments, CreatedAt from [{_storage.SchemaName}].Job with (readcommittedlock) where Id = @id";

            return _storage.UseConnection(connection =>
            {
                var jobData = connection.Query<SqlJob>(sql, new { id = long.Parse(id) }, commandTimeout: _storage.CommandTimeout)
                    .SingleOrDefault();

                if (jobData == null) return null;

                // TODO: conversion exception could be thrown.
                var invocationData = JobHelper.FromJson<InvocationData>(jobData.InvocationData);
                invocationData.Arguments = jobData.Arguments;

                Job job = null;
                JobLoadException loadException = null;

                try
                {
                    job = invocationData.Deserialize();
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

            string sql = 
$@"select s.Name, s.Reason, s.Data
from [{_storage.SchemaName}].State s with (readcommittedlock)
inner join [{_storage.SchemaName}].Job j with (readcommittedlock) on j.StateId = s.Id
where j.Id = @jobId";

            return _storage.UseConnection(connection =>
            {
                var sqlState = connection.Query<SqlState>(sql, new { jobId = long.Parse(jobId) }, commandTimeout: _storage.CommandTimeout).SingleOrDefault();
                if (sqlState == null)
                {
                    return null;
                }

                var data = new Dictionary<string, string>(
                    JobHelper.FromJson<Dictionary<string, string>>(sqlState.Data),
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

            _storage.UseConnection(connection =>
            {
                connection.Execute(
$@";merge [{_storage.SchemaName}].JobParameter with (holdlock) as Target
using (VALUES (@jobId, @name, @value)) as Source (JobId, Name, Value) 
on Target.JobId = Source.JobId AND Target.Name = Source.Name
when matched then update set Value = Source.Value
when not matched then insert (JobId, Name, Value) values (Source.JobId, Source.Name, Source.Value);",
                    new { jobId = long.Parse(id), name, value },
                    commandTimeout: _storage.CommandTimeout);
            });
        }

        public override string GetJobParameter(string id, string name)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            return _storage.UseConnection(connection => connection.ExecuteScalar<string>(
                $@"select top (1) Value from [{_storage.SchemaName}].JobParameter with (readcommittedlock) where JobId = @id and Name = @name",
                new { id = long.Parse(id), name = name },
                commandTimeout: _storage.CommandTimeout));
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _storage.UseConnection(connection =>
            {
                var result = connection.Query<string>(
                    $@"select Value from [{_storage.SchemaName}].[Set] with (readcommittedlock) where [Key] = @key",
                    new { key },
                    commandTimeout: _storage.CommandTimeout);

                return new HashSet<string>(result);
            });
        }

        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (toScore < fromScore) throw new ArgumentException("The `toScore` value must be higher or equal to the `fromScore` value.");

            return _storage.UseConnection(connection => connection.ExecuteScalar<string>(
                $@"select top 1 Value from [{_storage.SchemaName}].[Set] with (readcommittedlock) where [Key] = @key and Score between @from and @to order by Score",
                new { key, from = fromScore, to = toScore },
                commandTimeout: _storage.CommandTimeout));
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

            _storage.UseTransaction((connection, transaction) =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    connection.Execute(
                        sql, 
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value }, 
                        transaction,
                        commandTimeout: _storage.CommandTimeout);
                }
            });
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _storage.UseConnection(connection =>
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

            _storage.UseConnection(connection =>
            {
                connection.Execute(
$@";merge [{_storage.SchemaName}].Server with (holdlock) as Target
using (VALUES (@id, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat
when not matched then insert (Id, Data, LastHeartbeat) values (Source.Id, Source.Data, Source.Heartbeat);",
                    new { id = serverId, data = JobHelper.ToJson(data), heartbeat = DateTime.UtcNow },
                    commandTimeout: _storage.CommandTimeout);
            });
        }

        public override void RemoveServer(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));

            _storage.UseConnection(connection =>
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

            _storage.UseConnection(connection =>
            {
                connection.Execute(
                    $@"update [{_storage.SchemaName}].Server set LastHeartbeat = @now where Id = @id",
                    new { now = DateTime.UtcNow, id = serverId },
                    commandTimeout: _storage.CommandTimeout);
            });
        }

        public override int RemoveTimedOutServers(TimeSpan timeOut)
        {
            if (timeOut.Duration() != timeOut)
            {
                throw new ArgumentException("The `timeOut` value must be positive.", nameof(timeOut));
            }

            return _storage.UseConnection(connection => connection.Execute(
                $@"delete from [{_storage.SchemaName}].Server where LastHeartbeat < @timeOutAt",
                new { timeOutAt = DateTime.UtcNow.Add(timeOut.Negate()) },
                commandTimeout: _storage.CommandTimeout));
        }

        public override long GetSetCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _storage.UseConnection(connection => connection.Query<int>(
                $"select count([Key]) from [{_storage.SchemaName}].[Set] with (readcommittedlock) where [Key] = @key",
                new { key = key },
                commandTimeout: _storage.CommandTimeout).First());
        }

        public override List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query =
$@"select [Value] from (
	select [Value], row_number() over (order by [Id] ASC) as row_num
	from [{_storage.SchemaName}].[Set] with (readcommittedlock)
	where [Key] = @key 
) as s where s.row_num between @startingFrom and @endingAt";

            return _storage.UseConnection(connection => connection
                .Query<string>(query, new { key = key, startingFrom = startingFrom + 1, endingAt = endingAt + 1 }, commandTimeout: _storage.CommandTimeout)
                .ToList());
        }

        public override TimeSpan GetSetTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"select min([ExpireAt]) from [{_storage.SchemaName}].[Set] with (readcommittedlock) where [Key] = @key";

            return _storage.UseConnection(connection =>
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
$@"select sum(s.[Value]) from (select sum([Value]) as [Value] from [{_storage.SchemaName}].Counter with (readcommittedlock)
where [Key] = @key
union all
select [Value] from [{_storage.SchemaName}].AggregatedCounter with (readcommittedlock)
where [Key] = @key) as s";

            return _storage.UseConnection(connection => 
                connection.ExecuteScalar<long?>(query, new { key = key }, commandTimeout: _storage.CommandTimeout) ?? 0);
        }

        public override long GetHashCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"select count([Id]) from [{_storage.SchemaName}].Hash with (readcommittedlock) where [Key] = @key";

            return _storage.UseConnection(connection => connection.ExecuteScalar<long>(query, new { key = key }, commandTimeout: _storage.CommandTimeout));
        }

        public override TimeSpan GetHashTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = $@"select min([ExpireAt]) from [{_storage.SchemaName}].Hash with (readcommittedlock) where [Key] = @key";

            return _storage.UseConnection(connection =>
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
$@"select [Value] from [{_storage.SchemaName}].Hash with (readcommittedlock)
where [Key] = @key and [Field] = @field";

            return _storage.UseConnection(connection => connection
                .ExecuteScalar<string>(query, new { key = key, field = name }, commandTimeout: _storage.CommandTimeout));
        }

        public override long GetListCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = 
$@"select count([Id]) from [{_storage.SchemaName}].List with (readcommittedlock)
where [Key] = @key";

            return _storage.UseConnection(connection => connection.ExecuteScalar<long>(query, new { key = key }, commandTimeout: _storage.CommandTimeout));
        }

        public override TimeSpan GetListTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query = 
$@"select min([ExpireAt]) from [{_storage.SchemaName}].List with (readcommittedlock)
where [Key] = @key";

            return _storage.UseConnection(connection =>
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
	from [{_storage.SchemaName}].List with (readcommittedlock)
	where [Key] = @key 
) as s where s.row_num between @startingFrom and @endingAt";

            return _storage.UseConnection(connection => connection
                .Query<string>(query, new { key = key, startingFrom = startingFrom + 1, endingAt = endingAt + 1 }, commandTimeout: _storage.CommandTimeout)
                .ToList());
        }

        public override List<string> GetAllItemsFromList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            string query =
$@"select [Value] from [{_storage.SchemaName}].List with (readcommittedlock)
where [Key] = @key
order by [Id] desc";

            return _storage.UseConnection(connection => connection.Query<string>(query, new { key = key }, commandTimeout: _storage.CommandTimeout).ToList());
        }
    }
}
