using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.Storage;
using Sharp.Data;
using Sharp.Data.Exceptions;
using Sharp.Data.Filters;
using Sharp.Data.Schema;

namespace Hangfire.SharpData {
    public class SharpDataWriteOnlyTransaction : IWriteOnlyTransaction {
        private readonly IDataClient _client;

        private Queue<Action> _commands = new Queue<Action>();

        public SharpDataWriteOnlyTransaction(IDataClient client) {
            _client = client;
        }

        public void ExpireJob(string jobId, TimeSpan expireIn) {
            _commands.Enqueue(() => {
                _client.Update
                    .Table("Job")
                    .SetColumns("ExpireAt")
                    .ToValues(DateTime.UtcNow.Add(expireIn))
                    .Where(Filter.Eq("Id", jobId));
            });
        }

        public void PersistJob(string jobId) {
            _commands.Enqueue(() => {
                _client.Update
                    .Table("Job")
                    .SetColumns("ExpireAt")
                    .ToValues(DBNull.Value)
                    .Where(Filter.Eq("Id", jobId));
            });
        }

        public void SetJobState(string jobId, States.IState state) {
            _commands.Enqueue(() => {
                int id = InsertJobState(jobId, state);
                _client.Update
                    .Table("Job")
                    .SetColumns("StateId", "StateName")
                    .ToValues(id, state.Name)
                    .Where(Filter.Eq("Id", jobId));
            });
        }

        public void AddJobState(string jobId, States.IState state) {
            _commands.Enqueue(() => {
                InsertJobState(jobId, state);
            });
        }

        private int InsertJobState(string jobId, States.IState state) {
            return _client.Insert
                    .Into("State")
                    .Columns("JobId", "Name", "Reason", "CreatedAt", "Data")
                    .ValuesAnd(jobId, state.Name, state.Reason, DateTime.UtcNow, JobHelper.ToJson(state.SerializeData()))
                    .Return<Int32>("Id");
        }

        public void AddToQueue(string queue, string jobId) {
            throw new NotImplementedException();
        }

        public void IncrementCounter(string key) {
            IncrementCounterAction(key, 1);
        }

        public void IncrementCounter(string key, TimeSpan expireIn) {
            IncrementCounterAction(key, 1, expireIn);

        }

        public void DecrementCounter(string key) {
            IncrementCounterAction(key, -1);
        }

        public void DecrementCounter(string key, TimeSpan expireIn) {
            IncrementCounterAction(key, -1, expireIn);
        }

        private void IncrementCounterAction(string key, int increment, TimeSpan? expireIn = null) {
            var columns = new List<string> { "Key", "Value" };
            var values = new List<object> { key, increment };
            if (expireIn.HasValue) {
                columns.Add("ExpireAt");
                values.Add(DateTime.UtcNow.Add(expireIn.Value));
            }
            _commands.Enqueue(() => {
                _client.Insert
                    .Into("Counter")
                    .Columns(columns.ToArray())
                    .Values(values.ToArray());
            });
        }

        public void AddToSet(string key, string value) {
            AddToSet(key, value, 0.0);
        }

        public void AddToSet(string key, string value, double score) {
            _commands.Enqueue(() => {
                try {
                    _client.Insert
                       .Into("Set")
                       .Columns("Key", "Value", "Score")
                       .Values(key, value, score);
                }
                catch (UniqueConstraintException) {
                   _client.Update
                        .Table("Job")
                        .SetColumns("Score")
                        .ToValues(score)
                        .Where(CreateKeyValueFilter(key, value));
                }
            });
        }

        private Filter CreateKeyValueFilter(string key, string value) {
            var f1 = Filter.Eq("Key", key);
            var f2 = Filter.Eq("Value", value);
            return Filter.And(f1, f2);
        }

        public void RemoveFromSet(string key, string value) {
            _commands.Enqueue(() => {
                _client.Delete.From("Set").Where(CreateKeyValueFilter(key, value));
            });
        }

        public void InsertToList(string key, string value) {
            _commands.Enqueue(() => {
                _client.Insert
                       .Into("List")
                       .Columns("Key", "Value")
                       .Values(key, value);
            });
        }

        public void RemoveFromList(string key, string value) {
            _commands.Enqueue(() => {
                _client.Delete.From("List").Where(CreateKeyValueFilter(key, value));
            });
        }

        public void TrimList(string key, int keepStartingFrom, int keepEndingAt) {
//            keepStartingFrom++;
//            keepEndingAt++;
//            _commands.Enqueue(() => {
//                var res = _client
//                    .Delete
//                    .From("select * from List order by Id desc")
                    
//                _client.Insert
//                       .Into("List")
//                       .Columns("Key", "Value")
//                       .Values(key, value);
//            });

//            const string trimSql = @"
//with cte as (
//select row_number() over (order by Id desc) as row_num, [Key] from HangFire.List)
//delete from cte where row_num not between @start and @end and [Key] = @key";

//            QueueCommand(x => x.Execute(
//                trimSql,
//                new { key = key, start = keepStartingFrom + 1, end = keepEndingAt + 1 }));
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs) {
            throw new NotImplementedException();
        }

        public void RemoveHash(string key) {
            throw new NotImplementedException();
        }

        public void Commit() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}