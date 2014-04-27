using System;
using System.Threading;
using Common.Logging;
using Dapper;
using HangFire.Server;

namespace HangFire.SqlServer.Components
{
    internal class ExpirationManager2 : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExpirationManager2));
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
        private readonly TimeSpan _checkInterval;

        public ExpirationManager2(SqlServerStorage storage)
            : this(storage, TimeSpan.FromHours(1))
        {
        }

        public ExpirationManager2(SqlServerStorage storage, TimeSpan checkInterval)
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
