// This file is part of Hangfire. Copyright � 2013-2014 Sergey Odinokov.
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
    public interface IWriteOnlyTransaction : IDisposable
    {
        // Job operations
        void ExpireJob([NotNull] string jobId, TimeSpan expireIn);
        void PersistJob([NotNull] string jobId);
        void SetJobState([NotNull] string jobId, [NotNull] IState state);
        void AddJobState([NotNull] string jobId, [NotNull] IState state);

        // Queue operations
        void AddToQueue([NotNull] string queue, [NotNull] string jobId);

        // Counter operations
        void IncrementCounter([NotNull] string key);
        void IncrementCounter([NotNull] string key, TimeSpan expireIn);
        void DecrementCounter([NotNull] string key);
        void DecrementCounter([NotNull] string key, TimeSpan expireIn);

        // Set operations
        void AddToSet([NotNull] string key, [NotNull] string value);
        void AddToSet([NotNull] string key, [NotNull] string value, double score);
        void RemoveFromSet([NotNull] string key, [NotNull] string value);

        // List operations
        void InsertToList([NotNull] string key, [NotNull] string value);
        void RemoveFromList([NotNull] string key, [NotNull] string value);
        void TrimList([NotNull] string key, int keepStartingFrom, int keepEndingAt);

        // Hash operations
        void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        void RemoveHash([NotNull] string key);

        void Commit();
    }
}