﻿// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Storage
{
    public abstract class JobStorageTransaction : IWriteOnlyTransaction
    {
        public virtual void Dispose()
        {
        }

        public abstract void ExpireJob(string jobId, TimeSpan expireIn);
        public abstract void PersistJob(string jobId);
        public abstract void SetJobState(string jobId, IState state);
        public abstract void AddJobState(string jobId, IState state);
        public abstract void AddToQueue(string queue, string jobId);
        public abstract void IncrementCounter(string key);
        public abstract void IncrementCounter(string key, TimeSpan expireIn);
        public abstract void DecrementCounter(string key);
        public abstract void DecrementCounter(string key, TimeSpan expireIn);
        public abstract void AddToSet(string key, string value);
        public abstract void AddToSet(string key, string value, double score);
        public abstract void RemoveFromSet(string key, string value);
        public abstract void InsertToList(string key, string value);
        public abstract void RemoveFromList(string key, string value);
        public abstract void TrimList(string key, int keepStartingFrom, int keepEndingAt);
        public abstract void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        public abstract void RemoveHash(string key);
        public abstract void Commit();

        public virtual void ExpireSet([NotNull] string key, TimeSpan expireIn)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void ExpireList([NotNull] string key, TimeSpan expireIn)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void ExpireHash([NotNull] string key, TimeSpan expireIn)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void PersistSet([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void PersistList([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void PersistHash([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void AddRangeToSet([NotNull] string key, [NotNull] IList<string> items)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void RemoveSet([NotNull] string key)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.ExtendedApi);
        }

        public virtual void AcquireDistributedLock([NotNull] string resource, TimeSpan timeout)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Transaction.AcquireDistributedLock);
        }

        public virtual void RemoveFromQueue([NotNull] IFetchedJob fetchedJob)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Transaction.RemoveFromQueue(fetchedJob.GetType()));
        }
        
        public virtual void SetJobParameter([NotNull] string jobId, [NotNull] string name, [CanBeNull] string value)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Transaction.SetJobParameter);
        }

        [Obsolete("Please use/override the CreateJob method that contains `createdAt` parameter instead. Will be removed in 1.8.0.")]
        public virtual string CreateJob([NotNull] Job job, [NotNull] IDictionary<string, string> parameters, TimeSpan? expireIn)
        {
            throw JobStorageFeatures.GetNotSupportedException(JobStorageFeatures.Transaction.CreateJob);
        }

        public virtual string CreateJob([NotNull] Job job, [NotNull] IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
#pragma warning disable CS0618
            return CreateJob(job, parameters, expireIn);
#pragma warning restore CS0618
        }
    }
}
