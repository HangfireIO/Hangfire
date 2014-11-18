using System;
using System.Collections.Generic;
using Hangfire.Storage;
using Sharp.Data;

namespace Hangfire.SharpData {
    public class SharpDataWriteOnlyTransaction : IWriteOnlyTransaction {
        private readonly IDataClient _client;

        public SharpDataWriteOnlyTransaction(IDataClient client) {
            _client = client;
        }

        public void ExpireJob(string jobId, TimeSpan expireIn) {
            throw new NotImplementedException();
        }

        public void PersistJob(string jobId) {
            throw new NotImplementedException();
        }

        public void SetJobState(string jobId, States.IState state) {
            throw new NotImplementedException();
        }

        public void AddJobState(string jobId, States.IState state) {
            throw new NotImplementedException();
        }

        public void AddToQueue(string queue, string jobId) {
            throw new NotImplementedException();
        }

        public void IncrementCounter(string key) {
            throw new NotImplementedException();
        }

        public void IncrementCounter(string key, TimeSpan expireIn) {
            throw new NotImplementedException();
        }

        public void DecrementCounter(string key) {
            throw new NotImplementedException();
        }

        public void DecrementCounter(string key, TimeSpan expireIn) {
            throw new NotImplementedException();
        }

        public void AddToSet(string key, string value) {
            throw new NotImplementedException();
        }

        public void AddToSet(string key, string value, double score) {
            throw new NotImplementedException();
        }

        public void RemoveFromSet(string key, string value) {
            throw new NotImplementedException();
        }

        public void InsertToList(string key, string value) {
            throw new NotImplementedException();
        }

        public void RemoveFromList(string key, string value) {
            throw new NotImplementedException();
        }

        public void TrimList(string key, int keepStartingFrom, int keepEndingAt) {
            throw new NotImplementedException();
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