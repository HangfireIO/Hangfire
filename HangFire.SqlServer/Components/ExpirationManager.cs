using System;
using System.Data.SqlClient;
using System.Threading;
using Dapper;
using HangFire.Server;
using ServiceStack.Logging;

namespace HangFire.SqlServer.Components
{
    public class ExpirationManager : IThreadWrappable, IDisposable
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30); // TODO: increase interval

        private readonly SqlConnection _connection;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExpirationManager));
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private static readonly string[] ProcessedTables =
        {
            "Job",
            "Hash",
            "List",
            "Set",
            "Value"
        };

        public ExpirationManager(SqlConnection connection)
        {
            _connection = connection;
        }

        public void RemoveExpiredRecords()
        {
            foreach (var table in ProcessedTables)
            {
                _connection.Execute(
                    String.Format(@"delete from HangFire.[{0}] where ExpireAt < @now", table),
                    new { now = DateTime.UtcNow });
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
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
