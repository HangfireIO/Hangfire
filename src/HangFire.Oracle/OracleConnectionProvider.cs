using System.Data;
using Hangfire.Sql;
using OracleProvider = Oracle.ManagedDataAccess.Client;

namespace Hangfire.Oracle {
    public class OracleConnectionProvider : IConnectionProvider {
        private readonly string _connectionString;

        public OracleConnectionProvider(string connectionString) {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() {
            return new OracleProvider.OracleConnection(_connectionString);
        }
    }
}