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
using System.Threading;
using Common.Logging;
using Dapper;
using Hangfire.Server;

namespace Hangfire.SqlServer
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
            "Hash",
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
            using (var storageConnection = (SqlServerConnection)_storage.GetConnection())
            {
                foreach (var table in ProcessedTables)
                {
                    Logger.DebugFormat("Removing outdated records from table '{0}'...", table);

                    storageConnection.Connection.Execute(
                        String.Format(@"
set transaction isolation level read committed;
delete from HangFire.[{0}] with (tablock) where ExpireAt < @now;", table),
                        new { now = DateTime.UtcNow });
                }
            }

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        public override string ToString()
        {
            return "SQL Records Expiration Manager";
        }
    }
}
