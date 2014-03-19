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
