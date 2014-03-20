using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using Dapper;
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

            if (options.PrepareSchemaIfNecessary)
            {
                using (var connection = CreateAndOpenConnection())
                {
                    SqlServerObjectsInstaller.Install(connection);
                }
            }
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(CreateAndOpenConnection());
        }

        public override IStorageConnection GetConnection()
        {
            return new SqlServerConnection(this, CreateAndOpenConnection());
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(this, _options.QueuePollInterval);
            yield return new ServerWatchdog(this);
            yield return new ExpirationManager(this);
        }

        internal SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            return connection;
        }
    }
}