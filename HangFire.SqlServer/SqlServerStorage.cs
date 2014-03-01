using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HangFire.Server;
using HangFire.Server.Components;
using HangFire.SqlServer.Components;
using HangFire.Storage;
using HangFire.Storage.Monitoring;

namespace HangFire.SqlServer
{
    public class SqlServerStorage : JobStorage
    {
        private readonly SqlServerStorageOptions _options;
        private readonly string _connectionString;

        public SqlServerStorage(string connectionString)
            : this(connectionString, new SqlServerStorageOptions())
        {
        }

        public SqlServerStorage(string connectionString, SqlServerStorageOptions options)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (options == null) throw new ArgumentNullException("options");

            _options = options;
            _connectionString = connectionString;
        }

        // TODO: Long-running transaction will be killed
        public override IMonitoringApi Monitoring
        {
            get { return new SqlServerMonitoringApi(new SqlConnection(_connectionString)); }
        }

        public override IStorageConnection CreateConnection()
        {
            return CreatePooledConnection();
        }

        public override IStorageConnection CreatePooledConnection()
        {
            return new SqlStorageConnection(this, new SqlConnection(_connectionString));
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(CreateConnection(), _options.PollInterval);
            yield return new ServerWatchdog(CreateConnection());
            yield return new ExpirationManager(new SqlConnection(_connectionString));
        }
    }
}