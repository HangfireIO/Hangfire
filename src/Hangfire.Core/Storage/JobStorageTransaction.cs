// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Collections.Generic;
using Hangfire.Annotations;
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
            throw new NotSupportedException();
        }

        public virtual void ExpireList([NotNull] string key, TimeSpan expireIn)
        {
            throw new NotSupportedException();
        }

        public virtual void ExpireHash([NotNull] string key, TimeSpan expireIn)
        {
            throw new NotSupportedException();
        }

        public virtual void PersistSet([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual void PersistList([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual void PersistHash([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual void AddRangeToSet([NotNull] string key, [NotNull] IList<string> items)
        {
            throw new NotSupportedException();
        }

        public virtual void RemoveSet([NotNull] string key)
        {
            throw new NotSupportedException();
        }
    }
}