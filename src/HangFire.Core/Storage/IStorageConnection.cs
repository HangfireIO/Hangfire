// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading;
using HangFire.Common;
using HangFire.Server;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IWriteOnlyTransaction CreateWriteTransaction();

        string CreateExpiredJob(Job job, IDictionary<string, string> parameters, TimeSpan expireIn);

        IProcessingJob FetchNextJob(string[] queues, CancellationToken cancellationToken);
        void DeleteJobFromQueue(string jobId, string queue);

        void SetJobParameter(string id, string name, string value);
        string GetJobParameter(string id, string name);

        IDisposable AcquireJobLock(string jobId);
        JobData GetJobData(string id);

        string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);

        void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues);
        void RemoveServer(string serverId);
        void Heartbeat(string serverId);
        int RemoveTimedOutServers(TimeSpan timeOut);
    }
}