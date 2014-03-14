using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using HangFire.Server;
using HangFire.Server.Components;
using HangFire.SqlServer.Components;
using HangFire.Storage;
using HangFire.Storage.Monitoring;
using ServiceStack.Logging;

namespace HangFire.SqlServer
{
    public class SqlServerStorage : JobStorage
    {
        private const int RequiredSchemaVersion = 2;

        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerStorage));

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
                PrepareSchemaIfNecessary();
            }
        }

        public void PrepareSchemaIfNecessary()
        {
            Log.Debug("Start installing HangFire SQL objects...");

            if (!IsSqlEditionSupported(_connectionString))
            {
                throw new PlatformNotSupportedException("The SQL Server edition of the target server is unsupported, e.g. SQL Azure.");
            }

            var script = GetStringResource(GetType().Assembly, "HangFire.SqlServer.Install.sql");
            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 2;", "SET @TARGET_SCHEMA_VERSION = " + RequiredSchemaVersion + ";");

            using (var connection = CreateAndOpenConnection())
            {
                connection.Execute(script);
            }

            Log.Debug("HangFire SQL objects installed.");
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(CreateAndOpenConnection());
        }

        public override IStorageConnection GetConnection()
        {
            return new SqlStorageConnection(this, CreateAndOpenConnection());
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            yield return new SchedulePoller(this, _options.PollInterval);
            yield return new ServerWatchdog(this);
            yield return new ExpirationManager(this);
        }

        internal SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            return connection;
        }

        private bool IsSqlEditionSupported(string connectionString)
        {
            using (var connection = CreateAndOpenConnection())
            {
                var edition = connection.Query<int>("SELECT SERVERPROPERTY ( 'EngineEdition' )").Single();
                return edition >= SqlEngineEdition.Standard && edition <= SqlEngineEdition.Express;
            }
        }

        private static string GetStringResource(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static class SqlEngineEdition
        {
            // See article http://technet.microsoft.com/en-us/library/ms174396.aspx for details on EngineEdition
            public const int Personal = 1;
            public const int Standard = 2;
            public const int Enterprise = 3;
            public const int Express = 4;
            public const int SqlAzure = 5;
        }
    }
}