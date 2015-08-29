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
        private readonly IStorageConnection _connection;
        private readonly WorkerContext _workerContext;
        private readonly BackgroundProcessContext _backgroundProcessContext;

        public ServerJobCancellationToken(
            [NotNull] string jobId, 
            [NotNull] IStorageConnection connection, 
            [NotNull] WorkerContext workerContext, 
            [NotNull] BackgroundProcessContext backgroundProcessContext)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (connection == null) throw new ArgumentNullException("connection");
            if (workerContext == null) throw new ArgumentNullException("workerContext");
            if (backgroundProcessContext == null) throw new ArgumentNullException("backgroundProcessContext");

            _jobId = jobId;
            _connection = connection;
            _workerContext = workerContext;
            _backgroundProcessContext = backgroundProcessContext;
        }

        public CancellationToken ShutdownToken
        {
            get { return _backgroundProcessContext.CancellationToken; }
        }

        public void ThrowIfCancellationRequested()
        {
            _backgroundProcessContext.CancellationToken.ThrowIfCancellationRequested();

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

            if (!state.Data["ServerId"].Equals(_backgroundProcessContext.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data.ContainsKey("WorkerId"))
            {
                return true;
            }

            if (!state.Data["WorkerId"].Equals(_workerContext.WorkerId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}