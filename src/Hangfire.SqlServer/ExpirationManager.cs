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
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.SqlServer
{
#pragma warning disable 618
    internal class ExpirationManager : IServerComponent
#pragma warning restore 618
    {
        private static readonly ILog Logger = LogProvider.For<ExpirationManager>();

        private const string DistributedLockKey = "locks:expirationmanager";
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(5);

        // The value should be low enough to prevent INDEX SCAN operator,
        // see https://github.com/HangfireIO/Hangfire/issues/628
        private const int NumberOfRecordsInSinglePass = 100;
        
        private static readonly string[] ProcessedTables =
        {
            "AggregatedCounter",
            "Job",
            "List",
            "Set",
            "Hash",
        };

        private readonly SqlServerStorage _storage;
        private readonly TimeSpan _checkInterval;

        public ExpirationManager(SqlServerStorage storage, TimeSpan checkInterval)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            _storage = storage;
            _checkInterval = checkInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            foreach (var table in ProcessedTables)
            {
                Logger.Debug($"Removing outdated records from the '{table}' table...");

                _storage.UseConnection(connection =>
                {
                    SqlServerDistributedLock.Acquire(connection, DistributedLockKey, DefaultLockTimeout);

                    try
                    {
                        ExecuteNonQuery(
                            connection,
                            GetQuery(_storage.SchemaName, table),
                            cancellationToken,
                            new SqlParameter("@count", NumberOfRecordsInSinglePass),
                            new SqlParameter("@now", DateTime.UtcNow));
                    }
                    finally
                    {
                        SqlServerDistributedLock.Release(connection, DistributedLockKey);
                    }
                });

                Logger.Trace($"Outdated records removed from the '{table}' table.");
            }

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }

        private static string GetQuery(string schemaName, string table)
        {
            return
$@"set transaction isolation level read committed;
set nocount on;
while (1 = 1)
begin
    delete top (@count) from [{schemaName}].[{table}] with (readpast) where ExpireAt < @now;
    if @@ROWCOUNT = 0 break;
end";
        }

        private static int ExecuteNonQuery(
            DbConnection connection, 
            string commandText,
            CancellationToken cancellationToken,
            params SqlParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddRange(parameters);
                command.CommandTimeout = 0;

                var registration = cancellationToken.Register(command.Cancel);
                try
                {
                    return command.ExecuteNonQuery();
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }
    }
}
