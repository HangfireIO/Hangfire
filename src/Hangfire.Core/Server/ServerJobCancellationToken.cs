// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Globalization;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    internal class ServerJobCancellationToken : IJobCancellationToken
    {
        private readonly string _jobId;
        private readonly CancellationToken _shutdownToken;
        private readonly IStorageConnection _connection;
        private readonly WorkerContext _workerContext;

        public ServerJobCancellationToken(
            [NotNull] string jobId, 
            [NotNull] IStorageConnection connection, 
            [NotNull] WorkerContext workerContext,
            CancellationToken shutdownToken)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (connection == null) throw new ArgumentNullException("connection");
            if (workerContext == null) throw new ArgumentNullException("workerContext");

            _jobId = jobId;
            _shutdownToken = shutdownToken;
            _connection = connection;
            _workerContext = workerContext;
        }

        public CancellationToken ShutdownToken
        {
            get { return _shutdownToken; }
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
            var state = _connection.GetStateData(_jobId);

            if (state == null)
            {
                return true;
            }

            if (!state.Name.Equals(ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data["ServerId"].Equals(_workerContext.ServerId))
            {
                return true;
            }

            if (state.Data["WorkerNumber"] != _workerContext.WorkerNumber.ToString(CultureInfo.InvariantCulture))
            {
                return true;
            }

            return false;
        }
    }
}