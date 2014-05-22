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
using System.Threading;
using HangFire.States;
using HangFire.Storage;

namespace HangFire.Server
{
    internal class ServerJobCancellationToken : IJobCancellationToken
    {
        private readonly string _jobId;
        private readonly CancellationToken _shutdownToken;
        private readonly IStorageConnection _connection;

        public ServerJobCancellationToken(
            string jobId,
            IStorageConnection connection,
            CancellationToken shutdownToken)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (connection == null) throw new ArgumentNullException("connection");

            _jobId = jobId;
            _shutdownToken = shutdownToken;
            _connection = connection;
        }

        public void ThrowIfCancellationRequested()
        {
            _shutdownToken.ThrowIfCancellationRequested();

            if (IsJobAborted())
            {
                throw new JobAbortedException();
            }
        }

        private bool IsJobAborted()
        {
            var backgroundJob = _connection.GetJobData(_jobId);
            return !ProcessingState.StateName.Equals(backgroundJob.State, StringComparison.OrdinalIgnoreCase);
        }
    }
}