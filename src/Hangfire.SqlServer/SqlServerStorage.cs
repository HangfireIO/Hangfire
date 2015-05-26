// This file is part of Hangfire.
// Copyright � 2013-2014 Sergey Odinokov.
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
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    public class SqlServerStorage : JobStorage
    {
        private readonly SqlConnection _existingConnection;
        private readonly SqlServerStorageOptions _options;
        private readonly string _connectionString;

        public SqlServerStorage(string nameOrConnectionString)
            : this(nameOrConnectionString, new SqlServerStorageOptions())
        {
        }

        /// <summary>
        /// Initializes SqlServerStorage from the provided SqlServerStorageOptions and either the provided connection
        /// string or the connection string with provided name pulled from the application config file.       
        /// </summary>
        /// <param name="nameOrConnectionString">Either a SQL Server connection string or the name of 
        /// a SQL Server connection string located in the connectionStrings node in the application config</param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrConnectionString"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="nameOrConnectionString"/> argument is neither 
        /// a valid SQL Server connection string nor the name of a connection string in the application
        /// config file.</exception>
        public SqlServerStorage(string nameOrConnectionString, SqlServerStorageOptions options)
        {
            if (nameOrConnectionString == null) throw new ArgumentNullException("nameOrConnectionString");
            if (options == null) throw new ArgumentNullException("options");

            _options = options;

            if (IsConnectionString(nameOrConnectionString))
            {
                _connectionString = nameOrConnectionString;
            }
            else if (IsConnectionStringInConfiguration(nameOrConnectionString))
            {
                _connectionString = ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }
            else
            {
                throw new ArgumentException(
                    string.Format("Could not find connection string with name '{0}' in application config file",
                                  nameOrConnectionString));
            }

            if (options.PrepareSchemaIfNecessary)
            {
                using (var connection = CreateAndOpenConnection())
                {
                    SqlServerObjectsInstaller.Install(connection);
                }
            }

            InitializeQueueProviders();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// explicit instance of the <see cref="SqlConnection"/> class that will be used
        /// to query the data.
        /// </summary>
        /// <param name="existingConnection">Existing connection</param>
        public SqlServerStorage([NotNull] SqlConnection existingConnection)
        {
            if (existingConnection == null) throw new ArgumentNullException("existingConnection");

            _existingConnection = existingConnection;
            _options = new SqlServerStorageOptions();

            InitializeQueueProviders();
        }

        public PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(_connectionString, QueueProviders);
        }

        public override IStorageConnection GetConnection()
        {
            var connection = _existingConnection ?? CreateAndOpenConnection();
            return new SqlServerConnection(connection, _options.TransactionIsolationLevel, QueueProviders, _existingConnection == null);
        }

        public override IEnumerable<IServerComponent> GetComponents()
        {
            yield return new ExpirationManager(this, _options.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _options.CountersAggregateInterval);
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for SQL Server job storage:");
            logger.InfoFormat("    Queue poll interval: {0}.", _options.QueuePollInterval);
            logger.InfoFormat("    Invisibility timeout: {0}.", _options.InvisibilityTimeout);
        }

        public override string ToString()
        {
            const string canNotParseMessage = "<Connection string can not be parsed>";

            try
            {
                var parts = _connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => new { Key = x[0].Trim(), Value = x[1].Trim() })
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                var builder = new StringBuilder();

                foreach (var alias in new[] { "Data Source", "Server", "Address", "Addr", "Network Address" })
                {
                    if (parts.ContainsKey(alias))
                    {
                        builder.Append(parts[alias]);
                        break;
                    }
                }

                if (builder.Length != 0) builder.Append("@");

                foreach (var alias in new[] { "Database", "Initial Catalog" })
                {
                    if (parts.ContainsKey(alias))
                    {
                        builder.Append(parts[alias]);
                        break;
                    }
                }

                return builder.Length != 0
                    ? String.Format("SQL Server: {0}", builder)
                    : canNotParseMessage;
            }
            catch (Exception)
            {
                return canNotParseMessage;
            }
        }

        internal SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            return connection;
        }

        private void InitializeQueueProviders()
        {
            var defaultQueueProvider = new SqlServerJobQueueProvider(_options);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);
        }

        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }

        private bool IsConnectionStringInConfiguration(string connectionStringName)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }

        public static readonly DashboardMetric ActiveConnections = new DashboardMetric(
            "connections:active",
            "Active Connections",
            page =>
            {
                using (var connection = page.Storage.GetConnection())
                {
                    var sqlConnection = connection as SqlServerConnection;
                    if (sqlConnection == null) return new Metric("???");

                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background' and status != 'sleeping'";

                    var value = sqlConnection.Connection
                        .Query<int>(sqlQuery, new { name = sqlConnection.Connection.Database })
                        .Single();

                    return new Metric(value.ToString("N0"));
                }
            });

        public static readonly DashboardMetric TotalConnections = new DashboardMetric(
            "connections:total",
            "Total Connections",
            page =>
            {
                using (var connection = page.Storage.GetConnection())
                {
                    var sqlConnection = connection as SqlServerConnection;
                    if (sqlConnection == null) return new Metric("???");

                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background'";

                    var value = sqlConnection.Connection
                        .Query<int>(sqlQuery, new { name = sqlConnection.Connection.Database })
                        .Single();

                    return new Metric(value.ToString("N0"));
                }
            });
    }
}