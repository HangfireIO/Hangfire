// This file is part of Hangfire.
// Copyright © 2023 Hangfire OÜ.
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
using System.Collections.Concurrent;
using Hangfire.Annotations;

namespace Hangfire.Storage
{
    public static class JobStorageFeatures
    {
        internal static readonly string TransactionalAcknowledgePrefix = "TransactionalAcknowledge:";

        public static readonly string JobQueueProperty = "Job.Queue";
        public static readonly string ExtendedApi = "Storage.ExtendedApi";
        public static readonly string ProcessesInsteadOfComponents = "Storage.ProcessesInsteadOfComponents";

        public static class Connection
        {
            public static readonly string GetUtcDateTime = "Connection.GetUtcDateTime";
            public static readonly string GetSetContains = "Connection.GetSetContains";
            public static readonly string LimitedGetSetCount = "Connection.GetSetCount.Limited";

            public static readonly string BatchedGetFirstByLowest = "Connection.BatchedGetFirstByLowestScoreFromSet";
        }

        public static class Transaction
        {
            public static readonly string AcquireDistributedLock = "Transaction.AcquireDistributedLock";

            public static readonly string CreateJob = "Transaction.CreateJob";
            public static readonly string SetJobParameter = "Transaction.SetJobParameter";

            private static readonly ConcurrentDictionary<Type, string> RemoveFromQueueFeatureCache = new(); 

            public static string RemoveFromQueue(Type fetchedJobType)
            {
                return RemoveFromQueueFeatureCache.GetOrAdd(
                    fetchedJobType,
                    static type => TransactionalAcknowledgePrefix + type.Name);
            }
        }

        public static class Monitoring
        {
            public static readonly string DeletedStateGraphs = "Monitoring.DeletedStateGraphs";
            public static readonly string AwaitingJobs = "Monitoring.AwaitingJobs";
        }

        public static Exception GetNotSupportedException([NotNull] string featureId)
        {
            if (featureId == null) throw new ArgumentNullException(nameof(featureId));
            return new NotSupportedException(
                $"Current storage implementation does not support the '{featureId}' feature.");
        }
    }
}