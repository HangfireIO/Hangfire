using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SharpData.Entities;
using Hangfire.Storage;
using Sharp.Data;
using Sharp.Data.Exceptions;
using Sharp.Data.Filters;
using Sharp.Data.Schema;

namespace Hangfire.SharpData {
    public class SharpDataStorageConnection : IStorageConnection {
        private IDataClient _client;

        public SharpDataStorageConnection(string connectionString, string dataProviderName) {
            _client = SharpFactory.Default.CreateDataClient(connectionString, dataProviderName);
        }

        public IWriteOnlyTransaction CreateWriteTransaction() {
            return new SharpDataWriteOnlyTransaction(_client);
        }

        public IDistributedLock AcquireDistributedLock(string resource, TimeSpan timeout) {
            return new FakeDistributedLock();
        }

        public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn) {
            var invocationData = InvocationData.Serialize(job);
            var id = _client
                .Insert
                .Into("Job")
                .Columns("InvocationData", "Arguments", "CreatedAt", "ExpireAt")
                .ValuesAnd(
                    In.Named("invocationData", JobHelper.ToJson(invocationData)),
                    In.Named("arguments", invocationData.Arguments),
                    In.Named("createdAt", createdAt),
                    In.Named("expireAt", createdAt.Add(expireIn))
                ).Return<Int32>("Id");

            if (parameters.Count == 0) {
                return id.ToString();
            }
            var jobIds = Enumerable.Repeat(id, parameters.Count);
            var names = parameters.Keys.ToArray();
            var values = parameters.Values.ToArray();

            _client.Insert.Into("JobParameter")
                .Columns("JobId", "Name", "Value")
                .Values(jobIds, names, values);

            return id.ToString();
        }

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public void SetJobParameter(string id, string name, string value) {
            try {
                _client.Insert.Into("JobParameter")
                              .Columns("JobId", "Name", "Value")
                              .Values(id, name, value);
            }
            catch (UniqueConstraintException) {
                var jobIdFilter = Filter.Eq("JobId", id);
                var nameFilter = Filter.Eq("Name", name);
                var filter = Filter.And(jobIdFilter, nameFilter);
                _client.Update.Table("JobParameter").SetColumns("Value").ToValues(value).Where(filter);
            }
        }

        public string GetJobParameter(string id, string name) {
            var jobIdFilter = Filter.Eq("JobId", id);
            var nameFilter = Filter.Eq("Name", name);
            var filter = Filter.And(jobIdFilter, nameFilter);
            return _client.Select.Columns("Value").From("JobParameter").Where(filter).AllRows()[0][0].ToString();
        }

        public JobData GetJobData(string jobId) {
            var res = _client.Select
                .Columns("InvocationData", "StateName", "Arguments", "CreatedAt")
                .From("Job")
                .Where(Filter.Eq("Id", jobId))
                .AllRows();
            if (res.Count == 0) {
                return null;
            }
            var jobTuple = res[0];
            var invocationData = JobHelper.FromJson<InvocationData>(jobTuple["InvocationData"].ToString());
            invocationData.Arguments = jobTuple["Arguments"].ToString();
            Job job = null;
            JobLoadException loadException = null;
            try {
                job = invocationData.Deserialize();
            }
            catch (JobLoadException ex) {
                loadException = ex;
            }
            return new JobData {
                Job = job,
                State = jobTuple["StateName"].ToString(),
                CreatedAt = (DateTime)jobTuple["CreatedAt"],
                LoadException = loadException
            };
        }

        public StateData GetStateData(string jobId) {
            var sql = @"
select s.Name, s.Reason, s.Data
from HangFire.State s
inner join Job j on j.StateId = s.Id
where j.Id = " + jobId;
            var res = _client.Database.Query(sql);
            if (res.Count == 0) {
                return null;
            }
            return new StateData {
                Name = res[0]["Name"].ToString(),
                Reason = res[0]["Reason"].ToString(),
                Data = JobHelper.FromJson<Dictionary<string, string>>(res[0]["Data"].ToString())
            };
        }

        public void AnnounceServer(string serverId, ServerContext context) {
            var serverData = new ServerData {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues,
                StartedAt = DateTime.UtcNow,
            };
            var jsonData = JobHelper.ToJson(serverData);
            try {
                _client.Insert.Into("Server")
                              .Columns("Id", "Data", "LastHeartbeat")
                              .Values(serverId, jsonData, DateTime.UtcNow);
            }
            catch (UniqueConstraintException) {
                _client.Update
                    .Table("Server")
                    .SetColumns("Data", "LastHeartbeat")
                    .ToValues(jsonData, DateTime.UtcNow)
                    .Where(Filter.Eq("Id", serverId));
            }
        }

        public void RemoveServer(string serverId) {
            _client.Delete.From("Server").Where(Filter.Eq("Id", serverId));
        }

        public void Heartbeat(string serverId) {
            _client.Update
                .Table("Server")
                .SetColumns("LastHeartbeat")
                .ToValues(DateTime.UtcNow)
                .Where(Filter.Eq("Id", serverId));
        }

        public int RemoveTimedOutServers(TimeSpan timeOut) {
            if (timeOut.Duration() != timeOut) {
                throw new ArgumentException("The `timeOut` value must be positive.", "timeOut");
            }
            return _client.Delete
                .From("Server")
                .Where(Filter.Lt("LastHeartbeat", DateTime.UtcNow.Add(timeOut.Negate())));
        }

        public HashSet<string> GetAllItemsFromSet(string key) {
            if (key == null) throw new ArgumentNullException("key");

            var res = _client.Select.Columns("Value").From("Set").Where(Filter.Eq("Key", key)).AllRows();
            var items = res.Select(t => t[0].ToString());
            return new HashSet<string>(items);
        }

        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore) {
            if (key == null) throw new ArgumentNullException("key");
            if (toScore < fromScore) throw new ArgumentException("The `toScore` value must be higher or equal to the `fromScore` value.");

            var filterKey = Filter.Eq("Key", key);
            var filterFromScore = Filter.Ge("Score", fromScore);
            var filterToScore = Filter.Le("Score", toScore);
            var filter = Filter.And(filterKey, filterFromScore);
            filter = Filter.And(filter, filterToScore);
            var res = _client.Select
                .Columns("Value")
                .From("Set")
                .Where(filter)
                .OrderBy(OrderBy.Ascending("Score"))
                .SkipTake(0, 1);

            return res[0][0].ToString();
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs) {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            foreach (var keyValuePair in keyValuePairs) {
                try {
                    _client.Insert.Into("Hash")
                        .Columns("Key", "Field", "Value")
                        .Values(key, keyValuePair.Key, keyValuePair.Value);
                }
                catch (UniqueConstraintException) {
                    var f1 = Filter.Eq("Key", key);
                    var f2 = Filter.Eq("Field", keyValuePair.Key);
                    var filter = Filter.And(f1, f2);
                    _client.Update
                        .Table("Server")
                        .SetColumns("Value")
                        .ToValues(keyValuePair.Value)
                        .Where(filter);
                }
            }
        }

        public Dictionary<string, string> GetAllEntriesFromHash(string key) {
            if (key == null) throw new ArgumentNullException("key");

            var dic = _client.Select
                .Columns("Key")
                .From("Hash")
                .Where(Filter.Eq("Key", key))
                .AllRows()
                .ToDictionary(k => k["Field"].ToString(), v => v["Value"].ToString());

            return dic.Count != 0 ? dic : null;
        }
        public void Dispose() {
            _client.Dispose();
        }
    }
}