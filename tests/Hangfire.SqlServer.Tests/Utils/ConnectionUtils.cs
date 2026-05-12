using System;
using System.Data.Common;
using System.Data.SqlClient;
#if !NET452
using System.Threading.Tasks;
using Testcontainers.MsSql;
#endif

namespace Hangfire.SqlServer.Tests
{
    public static class ConnectionUtils
    {
        private const string DatabaseVariable = "Hangfire_SqlServer_DatabaseName";
        private const string ConnectionStringTemplateVariable 
            = "Hangfire_SqlServer_ConnectionStringTemplate";

        private const string MasterDatabaseName = "master";
        private const string DefaultDatabaseName =
#if NET452
                "Hangfire.SqlServer.Tests.net452"
#elif NET461
                "Hangfire.SqlServer.Tests.net461"
#elif NETCOREAPP3_1
                "Hangfire.SqlServer.Tests.netcoreapp3_1"
#elif NET6_0
                "Hangfire.SqlServer.Tests.net6_0"
#elif NET8_0
                "Hangfire.SqlServer.Tests.net8_0"
#else
                "Hangfire.SqlServer.Tests"
#endif
            ;
        private const string DefaultConnectionStringTemplate
            = @"Server=.\;Database={0};Trusted_Connection=True;TrustServerCertificate=True;";

        public static string GetDatabaseName()
        {
            return Environment.GetEnvironmentVariable(DatabaseVariable) ?? DefaultDatabaseName;
        }

        public static string GetMasterConnectionString()
        {
            return GetConnectionString(MasterDatabaseName);
        }

        public static string GetConnectionString()
        {
            return GetConnectionString(GetDatabaseName());
        }

        private static string GetConnectionString(string databaseName)
        {
            var connectionStringTemplate = Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable);
            if (!String.IsNullOrEmpty(connectionStringTemplate))
            {
                return String.Format(connectionStringTemplate, databaseName);
            }

#if NET452
            return String.Format(DefaultConnectionStringTemplate, databaseName);
#else
            return SqlServerTestcontainer.GetConnectionString(databaseName);
#endif
        }

        public static DbConnection CreateConnection(bool microsoftDataSqlClient)
        {
            var connection =
#if !NET452 && !NET461
                microsoftDataSqlClient ? (DbConnection)new Microsoft.Data.SqlClient.SqlConnection(GetConnectionString()) :
#endif
                new SqlConnection(GetConnectionString());

            connection.Open();

            return connection;
        }

#if !NET452
        private static class SqlServerTestcontainer
        {
            private static readonly Lazy<MsSqlContainer> Container = new Lazy<MsSqlContainer>(StartContainer);

            public static string GetConnectionString(string databaseName)
            {
                var builder = new SqlConnectionStringBuilder(Container.Value.GetConnectionString())
                {
                    InitialCatalog = databaseName
                };

                return builder.ConnectionString;
            }

            private static MsSqlContainer StartContainer()
            {
                var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
                    .Build();

                StartContainerAsync(container).GetAwaiter().GetResult();

                return container;
            }

            private static async Task StartContainerAsync(MsSqlContainer container)
            {
                await container.StartAsync().ConfigureAwait(false);
            }
        }
#endif
    }
}
