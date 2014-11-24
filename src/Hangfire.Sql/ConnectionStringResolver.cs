using System;
using System.Configuration;

namespace Hangfire.Sql {
    public static class ConnectionStringResolver {
        public static string GetConnectionString(string nameOrConnectionString) {
            if (IsConnectionString(nameOrConnectionString)) {
                return nameOrConnectionString;
            }
            if (IsConnectionStringInConfiguration(nameOrConnectionString)) {
                return ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }
            throw new ArgumentException(
                string.Format("Could not find connection string with name '{0}' in application config file",
                    nameOrConnectionString));
        }

        private static bool IsConnectionString(string nameOrConnectionString) {
            return nameOrConnectionString.Contains(";");
        }

        private static bool IsConnectionStringInConfiguration(string connectionStringName) {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }
    }
}