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
using System.Linq;
using Hangfire.Common;
using Hangfire.SqlServer.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Hangfire.SqlServer
{
    internal sealed class SqlServerMonitoringApi : JobStorageMonitor
    {
        private readonly SqlServerStorage _storage;
        private readonly int? _jobListLimit;

        public SqlServerMonitoringApi(SqlServerStorage storage, int? jobListLimit)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _jobListLimit = jobListLimit;
        }

        public override long ScheduledCount()
        {
            return _storage.UseConnection(static (storage, connection, api) =>
                GetNumberOfJobsByStateName(storage, connection, ScheduledState.StateName, api._jobListLimit),
                this);
        }

        public override long EnqueuedCount(string queue)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.EnqueuedCount ?? 0;
        }

        public override long FetchedCount(string queue)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.FetchedCount ?? 0;
        }

        public override long FailedCount()
        {
            return _storage.UseConnection(static (storage, connection, api) =>
                GetNumberOfJobsByStateName(storage, connection, FailedState.StateName, api._jobListLimit),
                this);
        }

        public override long ProcessingCount()
        {
            return _storage.UseConnection(static (storage, connection, api) =>
                GetNumberOfJobsByStateName(storage, connection, ProcessingState.StateName, api._jobListLimit),
                this);
        }

        public override JobList<ProcessingJobDto> ProcessingJobs(int from, int count)
        {
            return _storage.UseConnection(static (storage, connection, args) => GetJobs(
                storage,
                connection,
                args.Key, args.Value,
                ProcessingState.StateName,
                descending: false,
                static (sqlJob, job, invocationData, loadException, stateData) => new ProcessingJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    InProcessingState = ProcessingState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    ServerId = stateData.TryGetValue("ServerId", out var serverId) ? serverId : stateData["ServerName"],
                    StartedAt = sqlJob.StateChanged,
                    StateData = stateData
                }), new KeyValuePair<int, int>(from, count));
        }

        public override JobList<ScheduledJobDto> ScheduledJobs(int from, int count)
        {
            return _storage.UseConnection(static (storage, connection, args) => GetJobs(
                storage,
                connection,
                args.Key, args.Value,
                ScheduledState.StateName,
                descending: false,
                static (sqlJob, job, invocationData, loadException, stateData) => new ScheduledJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    InScheduledState = ScheduledState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    EnqueueAt = JobHelper.DeserializeNullableDateTime(stateData["EnqueueAt"]) ?? DateTime.MinValue,
                    ScheduledAt = sqlJob.StateChanged,
                    StateData = stateData
                }), new KeyValuePair<int, int>(from, count));
        }

        public override IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return _storage.UseConnection(static (storage, connection) => 
                GetTimelineStats(storage, connection, "succeeded"));
        }

        public override IDictionary<DateTime, long> FailedByDatesCount()
        {
            return _storage.UseConnection(static (storage, connection) => 
                GetTimelineStats(storage, connection, "failed"));
        }

        public override IDictionary<DateTime, long> DeletedByDatesCount()
        {
            return _storage.UseConnection(static (storage, connection) => 
                GetTimelineStats(storage, connection, "deleted"));
        }

        public override IList<ServerDto> Servers()
        {
            return _storage.UseConnection<IList<ServerDto>>(static (storage, connection) =>
            {
                var query = storage.GetQueryFromTemplate(static schemaName =>
                    $@"select [Id], [Data], [LastHeartbeat] from [{schemaName}].Server with (nolock)");

                using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout);

                var servers = command.ExecuteList(static reader => new Entities.Server
                {
                    Id = reader.GetRequiredString("Id"),
                    Data = reader.GetOptionalString("Data"),
                    LastHeartbeat = reader.GetRequiredDateTime("LastHeartbeat")
                });

                var result = new List<ServerDto>();

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var server in servers)
                {
                    var data = SerializationHelper.Deserialize<ServerData>(server.Data);

                    if (data?.Queues == null && data?.StartedAt == null && data?.WorkerCount == 0)
                    {
                        data = SerializationHelper.Deserialize<ServerData>(server.Data, SerializationOption.User);
                    }

                    result.Add(new ServerDto
                    {
                        Name = server.Id,
                        Heartbeat = server.LastHeartbeat,
                        Queues = data?.Queues ?? [],
                        StartedAt = data?.StartedAt ?? DateTime.MinValue,
                        WorkersCount = data?.WorkerCount ?? 0
                    });
                }

                return result;
            });
        }

        public override JobList<FailedJobDto> FailedJobs(int from, int count)
        {
            return _storage.UseConnection(static (storage, connection, args) => GetJobs(
                storage,
                connection,
                args.Key,
                args.Value,
                FailedState.StateName,
                descending: true,
                static (sqlJob, job, invocationData, loadException, stateData) => new FailedJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    InFailedState = FailedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    Reason = sqlJob.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = sqlJob.StateChanged,
                    StateData = stateData
                }), new KeyValuePair<int, int>(from, count));
        }

        public override JobList<SucceededJobDto> SucceededJobs(int from, int count)
        {
            return _storage.UseConnection(static (storage, connection, args) => GetJobs(
                storage,
                connection,
                args.Key,
                args.Value,
                SucceededState.StateName,
                descending: true,
                static (sqlJob, job, invocationData, loadException, stateData) => new SucceededJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    InSucceededState = SucceededState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    Result = stateData["Result"],
                    TotalDuration = stateData.TryGetValue("PerformanceDuration", out var duration) && stateData.TryGetValue("Latency", out var latency)
                        ? long.Parse(duration, CultureInfo.InvariantCulture) + (long?)long.Parse(latency, CultureInfo.InvariantCulture)
                        : null,
                    SucceededAt = sqlJob.StateChanged,
                    StateData = stateData
                }), new KeyValuePair<int, int>(from, count));
        }

        public override JobList<DeletedJobDto> DeletedJobs(int from, int count)
        {
            return _storage.UseConnection(static (storage, connection, args) => GetJobs(
                storage,
                connection,
                args.Key,
                args.Value,
                DeletedState.StateName,
                descending: true,
                static (sqlJob, job, invocationData, loadException, stateData) => new DeletedJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    InDeletedState = DeletedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    DeletedAt = sqlJob.StateChanged,
                    StateData = stateData
                }), new KeyValuePair<int, int>(from, count));
        }

        public override JobList<AwaitingJobDto> AwaitingJobs(int from, int count)
        {
            var awaitingJobs = _storage.UseConnection(static (storage, connection, args) => GetJobs(
                storage,
                connection,
                args.Key,
                args.Value,
                AwaitingState.StateName,
                descending: false,
                static (sqlJob, job, invocationData, loadException, stateData) => new AwaitingJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    InAwaitingState = AwaitingState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    AwaitingAt = sqlJob.StateChanged,
                    StateData = stateData
                }), new KeyValuePair<int, int>(from, count));

            var parentIds = awaitingJobs
                .Where(static x => x.Value != null && x.Value.InAwaitingState && x.Value.StateData!.ContainsKey("ParentId"))
                .Select(static x => long.Parse(x.Value!.StateData!["ParentId"], CultureInfo.InvariantCulture))
                .ToArray();

            var parentStates = parentIds.Length > 0 
                ? _storage.UseConnection(static (storage, connection, parentIds) =>
                {
                    var query = storage.GetQueryFromTemplate(static schemaName =>
                        $@"select Id, StateName from [{schemaName}].Job with (nolock, forceseek) where Id in @ids");

                    using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                        .AddExpandedParameter("@ids", parentIds, DbType.Int64);

                    return command.ExecuteList(static reader => new ParentStateDto
                    {
                        Id = reader.GetRequiredValue<long>("Id"),
                        StateName = reader.GetOptionalString("StateName")
                    }).ToDictionary(static x => x.Id, static x => x.StateName);
                }, parentIds)
                : new Dictionary<long, string?>();

            foreach (var awaitingJob in awaitingJobs)
            {
                if (awaitingJob.Value != null && awaitingJob.Value.InAwaitingState && awaitingJob.Value!.StateData!.TryGetValue("ParentId", out var parentIdString))
                {
                    var parentId = long.Parse(parentIdString, CultureInfo.InvariantCulture);
                    if (parentStates.TryGetValue(parentId, out var parentStateName))
                    {
                        awaitingJob.Value.ParentStateName = parentStateName;
                    }
                }
            }

            return awaitingJobs;
        }

        public override long AwaitingCount()
        {
            return _storage.UseConnection(static (storage, connection, api) =>
                GetNumberOfJobsByStateName(storage, connection, AwaitingState.StateName, api._jobListLimit),
                this);
        }

        public override IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            var tuples = _storage.QueueProviders
                .Select(static x => x.GetJobQueueMonitoringApi())
                .SelectMany(static x => x.GetQueues(), static (monitoring, queue) => new { Monitoring = monitoring, Queue = queue })
                .OrderBy(static x => x.Queue)
                .ToArray();

            var result = new List<QueueWithTopEnqueuedJobsDto>(tuples.Length);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var tuple in tuples)
            {
                var enqueuedJobIds = tuple.Monitoring.GetEnqueuedJobIds(tuple.Queue, 0, 5).ToArray();
                var counters = tuple.Monitoring.GetEnqueuedAndFetchedCount(tuple.Queue);

                var firstJobs = enqueuedJobIds.Length > 0 
                    ? _storage.UseConnection(static (storage, connection, args) => EnqueuedJobs(storage, connection, args), enqueuedJobIds)
                    : new JobList<EnqueuedJobDto>();

                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = tuple.Queue,
                    Length = counters.EnqueuedCount ?? 0,
                    Fetched = counters.FetchedCount,
                    FirstJobs = firstJobs
                });
            }

            return result;
        }

        public override JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage).ToArray();

            return enqueuedJobIds.Length > 0
                ? _storage.UseConnection(static (storage, connection, args) => EnqueuedJobs(storage, connection, args), enqueuedJobIds)
                : new JobList<EnqueuedJobDto>();
        }

        public override JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage).ToArray();

            return fetchedJobIds.Length > 0
                ? _storage.UseConnection(static (storage, connection, args) => FetchedJobs(storage, connection, args), fetchedJobIds)
                : new JobList<FetchedJobDto>();
        }

        public override IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return _storage.UseConnection(static (storage, connection) => 
                GetHourlyTimelineStats(storage, connection, "succeeded"));
        }

        public override IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return _storage.UseConnection(static (storage, connection) => 
                GetHourlyTimelineStats(storage, connection, "failed"));
        }

        public override IDictionary<DateTime, long> HourlyDeletedJobs()
        {
            return _storage.UseConnection(static (storage, connection) => 
                GetHourlyTimelineStats(storage, connection, "deleted"));
        }

        public override JobDetailsDto? JobDetails(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            if (!long.TryParse(jobId, out var parsedId))
            {
                return null;
            }

            return _storage.UseConnection(static (storage, connection, parsedId) =>
            {
                var query = storage.GetQueryFromTemplate(static schemaName => $@"
select InvocationData, Arguments, CreatedAt, ExpireAt from [{schemaName}].Job with (nolock, forceseek) where Id = @id
select Name, Value from [{schemaName}].JobParameter with (nolock, forceseek) where JobId = @id
select Name, Reason, Data, CreatedAt from [{schemaName}].State with (nolock, forceseek) where JobId = @id order by Id desc");

                using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                    .AddParameter("@id", parsedId, DbType.Int64);

                using var multi = command.ExecuteMultiple();
                var job = multi.ReadSingleOrDefaultAndProceed(static reader => new SqlJob
                {
                    InvocationData = reader.GetRequiredString("InvocationData"),
                    Arguments = reader.GetOptionalString("Arguments"),
                    CreatedAt = reader.GetRequiredDateTime("CreatedAt"),
                    ExpireAt = reader.GetOptionalDateTime("ExpireAt")
                });
                if (job == null) return null;

                var parameters = multi.ReadListAndProceed(static reader => new KeyValuePair<string, string?>(
                        reader.GetRequiredString("Name"), reader.GetOptionalString("Value")))
                    .GroupBy(static x => x.Key)
                    .Select(static grp => grp.First())
                    .ToDictionary(static x => x.Key, static x => x.Value);

                var deserializedJob = DeserializeJob(job.InvocationData!, job.Arguments, out var payload, out var exception);

                if (deserializedJob == null)
                {
                    if (payload != null)
                    {
                        parameters.Add("DBG_Type", payload.Type);
                        parameters.Add("DBG_Method", payload.Method);
                        parameters.Add("DBG_Args", payload.Arguments);
                    }
                    else
                    {
                        parameters.Add("DBG_Payload", job.InvocationData);
                        parameters.Add("DBG_Args", job.Arguments);
                    }
                }

                if (exception != null)
                {
                    parameters.Add("DBG_Exception", (exception.InnerException ?? exception).Message);
                }

                var history = multi.ReadListAndFinish(static reader => new StateHistoryDto
                    {
                        StateName = reader.GetRequiredString("Name"),
                        Reason = reader.GetOptionalString("Reason"),
                        Data = new SafeDictionary<string, string>(
                            SerializationHelper.Deserialize<Dictionary<string, string>>(reader.GetOptionalString("Data")) ?? [],
                            StringComparer.OrdinalIgnoreCase),
                        CreatedAt = reader.GetRequiredDateTime("CreatedAt")
                    });

                return new JobDetailsDto
                {
                    CreatedAt = job.CreatedAt,
                    ExpireAt = job.ExpireAt,
                    Job = deserializedJob,
                    InvocationData = payload!,
                    LoadException = exception,
                    History = history,
                    Properties = parameters
                };
            }, parsedId);
        }

        public override long SucceededListCount()
        {
            return _storage.UseConnection(static (storage, connection, api) =>
                GetNumberOfJobsByStateName(storage, connection, SucceededState.StateName, api._jobListLimit),
                this);
        }

        public override long DeletedListCount()
        {
            return _storage.UseConnection(static (storage, connection, api) =>
                GetNumberOfJobsByStateName(storage, connection, DeletedState.StateName, api._jobListLimit),
                this);
        }

        public override StatisticsDto GetStatistics()
        {
            var statistics = _storage.UseConnection(static (storage, connection) =>
            {
                var query = storage.GetQueryFromTemplate(static schemaName => $@"
set transaction isolation level read committed;
select count(Id) from [{schemaName}].Job with (nolock, forceseek) where StateName = N'Enqueued';
select count(Id) from [{schemaName}].Job with (nolock, forceseek) where StateName = N'Failed';
select count(Id) from [{schemaName}].Job with (nolock, forceseek) where StateName = N'Processing';
select count(Id) from [{schemaName}].Job with (nolock, forceseek) where StateName = N'Scheduled';
select count(Id) from [{schemaName}].Job with (nolock, forceseek) where StateName = N'Awaiting';
select count(Id) from [{schemaName}].Server with (nolock);
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{schemaName}].Counter with (nolock, forceseek) where [Key] = N'stats:succeeded'
    union all
    select [Value] from [{schemaName}].AggregatedCounter with (nolock, forceseek) where [Key] = N'stats:succeeded'
) as s;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{schemaName}].Counter with (nolock, forceseek) where [Key] = N'stats:deleted'
    union all
    select [Value] from [{schemaName}].AggregatedCounter with (nolock, forceseek) where [Key] = N'stats:deleted'
) as s;

select count(*) from [{schemaName}].[Set] with (nolock, forceseek) where [Key] = N'recurring-jobs';
select count(*) from [{schemaName}].[Set] with (nolock, forceseek) where [Key] = N'retries';");

                using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout);
                using var multi = command.ExecuteMultiple();

                return new StatisticsDto
                {
                    Enqueued = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Failed = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Processing = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Scheduled = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Awaiting = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Servers = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Succeeded = multi.ReadSingleAndProceed(static r => r.GetOptionalValue<long?>()) ?? 0,
                    Deleted = multi.ReadSingleAndProceed(static r => r.GetOptionalValue<long?>()) ?? 0,
                    Recurring = multi.ReadSingleAndProceed(static r => r.GetRequiredValue<int>()),
                    Retries = multi.ReadSingleAndFinish(static r => r.GetRequiredValue<int>())
                };
            });

            statistics.Queues = _storage.QueueProviders
                .SelectMany(static x => x.GetJobQueueMonitoringApi().GetQueues())
                .Count();

            return statistics;
        }

        private static Dictionary<DateTime, long> GetHourlyTimelineStats(SqlServerStorage storage, DbConnection connection, string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd-HH", CultureInfo.InvariantCulture)}", static x => x);

            return GetTimelineStats(storage, connection, keyMaps);
        }

        private static Dictionary<DateTime, long> GetTimelineStats(SqlServerStorage storage, DbConnection connection, string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}", static x => x);

            return GetTimelineStats(storage, connection, keyMaps);
        }

        private static Dictionary<DateTime, long> GetTimelineStats(
            SqlServerStorage storage,
            DbConnection connection,
            IDictionary<string, DateTime> keyMaps)
        {
            if (keyMaps.Count == 0) return new Dictionary<DateTime, long>();

            var query = storage.GetQueryFromTemplate(static schemaName =>
$@"select [Key], [Value] as [Count] from [{schemaName}].AggregatedCounter with (nolock, forceseek)
where [Key] in @keys");

            using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                .AddExpandedParameter("@keys", keyMaps.Keys.ToArray(), DbType.String);

            var valuesMap = command.ExecuteList(static reader => new KeyValuePair<string, long>(
                reader.GetRequiredString("Key"),
                reader.GetRequiredValue<long>("Count"))) // TODO: By mistake was Value, not ended with test failure
                .ToDictionary(static x => x.Key, static x => x.Value);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            var provider = _storage.QueueProviders.GetProvider(queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi();

            return monitoringApi;
        }

        private static JobList<EnqueuedJobDto> EnqueuedJobs(SqlServerStorage storage, DbConnection connection, long[] jobIds)
        {
            var query = storage.GetQueryFromTemplate(static schemaName =>
$@"select j.Id, j.StateName, j.InvocationData, j.Arguments, j.CreatedAt, j.ExpireAt,
  s.Reason as StateReason, s.Data as StateData, s.CreatedAt as StateChanged
from [{schemaName}].Job j with (nolock, forceseek)
left join [{schemaName}].State s with (nolock, forceseek) on s.Id = j.StateId and s.JobId = j.Id
where j.Id in @jobIds");

            using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                .AddExpandedParameter("@jobIds", jobIds, DbType.Int64);

            var jobs = command.ExecuteList(static reader => new SqlJob
            {
                Id = reader.GetRequiredValue<long>("Id"),
                StateName = reader.GetOptionalString("StateName"),
                InvocationData = reader.GetRequiredString("InvocationData"),
                Arguments = reader.GetOptionalString("Arguments"),
                CreatedAt = reader.GetRequiredDateTime("CreatedAt"),
                ExpireAt = reader.GetOptionalDateTime("ExpireAt"),
                StateReason = reader.GetOptionalString("StateReason"),
                StateData = reader.GetOptionalString("StateData"),
                StateChanged = reader.GetOptionalDateTime("StateChanged")
            }).ToDictionary(static x => x.Id, static x => x); // TODO: Should remove duplicates first to avoid exception?

            var sortedSqlJobs = jobIds
                .Select(jobId => jobs.TryGetValue(jobId, out var job) ? job : new SqlJob { Id = jobId })
                .ToList();
            
            return DeserializeJobs(
                sortedSqlJobs,
                static (sqlJob, job, invocationData, loadException, stateData) => new EnqueuedJobDto
                {
                    Job = job,
                    LoadException = loadException,
                    InvocationData = invocationData,
                    State = sqlJob.StateName,
                    InEnqueuedState = EnqueuedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase),
                    EnqueuedAt = EnqueuedState.StateName.Equals(sqlJob.StateName, StringComparison.OrdinalIgnoreCase)
                        ? sqlJob.StateChanged
                        : null,
                    StateData = stateData
                });
        }

        private static long GetNumberOfJobsByStateName(SqlServerStorage storage, DbConnection connection, string stateName, int? jobListLimit)
        {
            var query = storage.GetQueryFromTemplate(jobListLimit.HasValue
                ? static schemaName => $@"select count(j.Id) from (select top (@limit) Id from [{schemaName}].Job with (nolock, forceseek) where StateName = @state) as j"
                : static schemaName => $@"select count(Id) from [{schemaName}].Job with (nolock, forceseek) where StateName = @state");

            using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                .AddParameter("@state", stateName, DbType.String)
                .AddParameter("@limit", jobListLimit ?? -1, DbType.Int32);

            return command.ExecuteScalar<int>();
        }

        private static Job? DeserializeJob(string invocationData, string? arguments, out InvocationData data, out JobLoadException? exception)
        {
            data = InvocationData.DeserializePayload(invocationData);

            if (!String.IsNullOrEmpty(arguments))
            {
                data.Arguments = arguments;
            }

            try
            {
                exception = null;
                return data.DeserializeJob();
            }
            catch (JobLoadException ex)
            {
                exception = ex;
                return null;
            }
        }

        private static JobList<TDto> GetJobs<TDto>(
            SqlServerStorage storage,
            DbConnection connection,
            int from,
            int count,
            string stateName,
            bool descending,
            Func<SqlJob, Job?, InvocationData, JobLoadException?, SafeDictionary<string, string>, TDto> selector)
        {
            var order = descending ? "desc" : "asc";

            // TODO: Better to split into two queries to avoid allocating strings on each query
            var query = storage.GetQueryFromTemplate(schemaName => GetJobsQuery(schemaName, order));

            using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                .AddParameter("@stateName", stateName, DbType.String)
                .AddParameter("@start", from + 1, DbType.Int32)
                .AddParameter("@end", from + count, DbType.Int32);

            var jobs = command.ExecuteList(static reader => new SqlJob
            {
                Id = reader.GetRequiredValue<long>("Id"),
                StateName = reader.GetOptionalString("StateName"),
                InvocationData = reader.GetRequiredString("InvocationData"),
                Arguments = reader.GetOptionalString("Arguments"),
                CreatedAt = reader.GetRequiredDateTime("CreatedAt"),
                ExpireAt = reader.GetOptionalDateTime("ExpireAt"),
                StateReason = reader.GetOptionalString("StateReason"),
                StateData = reader.GetOptionalString("StateData"),
                StateChanged = reader.GetOptionalDateTime("StateChanged")
            });

            return DeserializeJobs(jobs, selector);
        }

        private static string GetJobsQuery(string schemaName, string order)
        {
            return String.Format(CultureInfo.InvariantCulture, 
$@";with cte as 
(
  select j.Id, row_number() over (order by j.Id {{0}}) as row_num
  from [{schemaName}].Job j with (nolock, forceseek)
  where j.StateName = @stateName
)
select j.Id, j.StateName, j.InvocationData, j.Arguments, j.CreatedAt, j.ExpireAt,
  s.Reason as StateReason, s.Data as StateData, s.CreatedAt as StateChanged
from [{schemaName}].Job j with (nolock, forceseek)
inner join cte on cte.Id = j.Id
left join [{schemaName}].State s with (nolock, forceseek) on j.StateId = s.Id and j.Id = s.JobId
where cte.row_num between @start and @end", order);
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job?, InvocationData, JobLoadException?, SafeDictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto?>>(jobs.Count);
            
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);
                
                if (job.InvocationData != null)
                {
                    var deserializedData = SerializationHelper.Deserialize<Dictionary<string, string>>(job.StateData);
                    var stateData = deserializedData != null
                        ? new SafeDictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : new SafeDictionary<string, string>();

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments, out var invocationData, out var loadException), invocationData, loadException, stateData);
                }

                result.Add(new KeyValuePair<string, TDto?>(
                    job.Id.ToString(CultureInfo.InvariantCulture), dto));
            }

            return new JobList<TDto>(result);
        }

        private static JobList<FetchedJobDto> FetchedJobs(SqlServerStorage storage, DbConnection connection, long[] jobIds)
        { 
            if (jobIds.Length == 0) return new JobList<FetchedJobDto>();

            var query = storage.GetQueryFromTemplate(static schemaName =>
$@"select j.Id, j.StateName, j.InvocationData, j.Arguments, j.CreatedAt, j.ExpireAt, s.Reason as StateReason, s.Data as StateData 
from [{schemaName}].Job j with (nolock, forceseek)
left join [{schemaName}].State s with (nolock, forceseek) on s.Id = j.StateId and s.JobId = j.Id
where j.Id in @jobIds");

            using var command = connection.CreateCommand(query, timeout: storage.CommandTimeout)
                .AddExpandedParameter("@jobIds", jobIds, DbType.Int64);

            var jobs = command.ExecuteList(static reader => new SqlJob
            {
                Id = reader.GetRequiredValue<long>("Id"),
                StateName = reader.GetOptionalString("StateName"),
                InvocationData = reader.GetRequiredString("InvocationData"),
                Arguments = reader.GetOptionalString("Arguments"),
                CreatedAt = reader.GetRequiredDateTime("CreatedAt"),
                ExpireAt = reader.GetOptionalDateTime("ExpireAt"),
                StateReason = reader.GetOptionalString("StateReason"),
                StateData = reader.GetOptionalString("StateData"),
                StateChanged = reader.GetOptionalDateTime("StateChanged")
            });

            var result = new List<KeyValuePair<string, FetchedJobDto?>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, FetchedJobDto?>(
                    job.Id.ToString(CultureInfo.InvariantCulture),
                    new FetchedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData!, job.Arguments, out _, out _),
                        State = job.StateName,
                    }));
            }

            return new JobList<FetchedJobDto>(result);
        }

        /// <summary>
        /// Overloaded dictionary that doesn't throw if given an invalid key
        /// Fixes issues such as https://github.com/HangfireIO/Hangfire/issues/871
        /// </summary>
        private sealed class SafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
            where TKey : notnull
        {
            public SafeDictionary()
            {
            }

            public SafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) 
                : base(dictionary, comparer)
            {
            }

            public new TValue? this[TKey i] => TryGetValue(i, out var value) ? value : default(TValue);
        }

        private sealed class ParentStateDto
        {
            public long Id { get; set; }
            public string? StateName { get; set; }
        }
    }
}

