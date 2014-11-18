using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Sharp.Data;
using Sharp.Data.Exceptions;
using Sharp.Data.Filters;

namespace Hangfire.SharpData {
    public class SharpDataStorageConnection : IStorageConnection {
        private IDataClient _client;

        public SharpDataStorageConnection(string connectionString, string dataProviderName) {
            _client = SharpFactory.Default.CreateDataClient(connectionString, dataProviderName);
        }

        public IWriteOnlyTransaction CreateWriteTransaction() {
            return new SharpDataWriteOnlyTransaction(_client);
        }

        public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout) {
            throw new NotSupportedException("This provider doesn't support distributed locks for database " + _client.Database.Provider.Name);
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
            //try {
            //    _client.Insert.Into("Server")
            //                  .Columns("JobId", "Name", "Value")
            //                  .Values(id, name, value);
            //}
            //catch (UniqueConstraintException) {
            //    var jobIdFilter = Filter.Eq("JobId", id);
            //    var nameFilter = Filter.Eq("Name", name);
            //    var filter = Filter.And(jobIdFilter, nameFilter);
            //    _client.Update.Table("JobParameter").SetColumns("Value").ToValues(value).Where(filter);
            //}
        }

        public void RemoveServer(string serverId) {
            throw new NotImplementedException();
        }

        public void Heartbeat(string serverId) {
            throw new NotImplementedException();
        }

        public int RemoveTimedOutServers(TimeSpan timeOut) {
            throw new NotImplementedException();
        }

        public HashSet<string> GetAllItemsFromSet(string key) {
            throw new NotImplementedException();
        }

        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore) {
            throw new NotImplementedException();
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs) {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetAllEntriesFromHash(string key) {
            throw new NotImplementedException();
        }
        public void Dispose() {
            _client.Dispose();
        }
    }
}