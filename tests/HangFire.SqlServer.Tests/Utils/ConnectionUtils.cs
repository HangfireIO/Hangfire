using System;
using System.Data.SqlClient;

namespace Hangfire.SqlServer.Tests
{
    public static class ConnectionUtils
    {
        private const string DatabaseVariable = "Hangfire_SqlServer_DatabaseName";
        private const string ConnectionStringTemplateVariable 
            = "Hangfire_SqlServer_ConnectionStringTemplate";

        private const string MasterDatabaseName = "master";
        private const string DefaultDatabaseName = @"Hangfire.SqlServer.Tests";
        private const string DefaultConnectionStringTemplate
            = @"Server=.\sqlexpress;Database={0};Trusted_Connection=True;";

        public static string GetDatabaseName()
        {
            return Environment.GetEnvironmentVariable(DatabaseVariable) ?? DefaultDatabaseName;
        }

        public static string GetMasterConnectionString()
        {
            return String.Format(GetConnectionStringTemplate(), MasterDatabaseName);
        }

        public static string GetConnectionString()
        {
            return String.Format(GetConnectionStringTemplate(), GetDatabaseName());
        }

        private static string GetConnectionStringTemplate()
        {
            return Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable)
                   ?? DefaultConnectionStringTemplate;
        }

        public static SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(GetConnectionString());
            connection.Open();

            return connection;
        }
    }
}
