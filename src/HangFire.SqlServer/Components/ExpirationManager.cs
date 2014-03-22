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
using Common.Logging;
using Dapper;
using HangFire.Server;

namespace HangFire.SqlServer.Components
{
    internal class ExpirationManager : IThreadWrappable
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExpirationManager));
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private static readonly string[] ProcessedTables =
        {
            "Job",
            "Hash",
            "List",
            "Set",
            "Value",
            "Counter",
        };

        private readonly SqlServerStorage _storage;

        public ExpirationManager(SqlServerStorage storage)
        {
            _storage = storage;
        }

        public void RemoveExpiredRecords()
        {
            using (var connection = _storage.CreateAndOpenConnection())
            {
                foreach (var table in ProcessedTables)
                {
                    connection.Execute(
                        String.Format(@"
set transaction isolation level read committed;
delete from HangFire.[{0}] with (tablock) where ExpireAt < @now;", table),
                        new { now = DateTime.UtcNow });
                }
            }
        }

        void IThreadWrappable.Work()
        {
            try
            {
                Logger.Info("Expiration manager has been started.");

                while (true)
                {
                    JobServer.RetryOnException(
                        RemoveExpiredRecords,
                        _stopped);

                    if (_stopped.WaitOne(CheckInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Expiration manager has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected exception caught.", ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _stopped.Set();
            thread.Join();
        }
    }
}
