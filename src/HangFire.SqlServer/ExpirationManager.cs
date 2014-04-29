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

namespace HangFire.SqlServer
{
    internal class ExpirationManager : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExpirationManager));
        private static readonly string[] ProcessedTables =
        {
            "Counter",
            "Job",
            "List",
            "Set",
        };

        private readonly SqlServerStorage _storage;
        private readonly TimeSpan _checkInterval;

        public ExpirationManager(SqlServerStorage storage)
            : this(storage, TimeSpan.FromHours(1))
        {
        }

        public ExpirationManager(SqlServerStorage storage, TimeSpan checkInterval)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _storage = storage;
            _checkInterval = checkInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.CreateAndOpenConnection())
            {
                foreach (var table in ProcessedTables)
                {
                    Logger.DebugFormat("Removing outdated records from table '{0}'...", table);

                    connection.Execute(
                        String.Format(@"
set transaction isolation level read committed;
delete from HangFire.[{0}] with (tablock) where ExpireAt < @now;", table),
                        new { now = DateTime.UtcNow });
                }
            }

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }
    }
}
