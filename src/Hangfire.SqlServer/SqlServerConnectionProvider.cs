using System.Data;
using Hangfire.Sql;

namespace Hangfire.SqlServer {
    public class SqlStorageConnectionProvider : IConnectionProvider {
        private readonly string _connectionString;

        public SqlStorageConnectionProvider(string connectionString) {
            _connectionString = connectionString;
        }

        public IDbConnection CreateAndOpenConnection() {
            var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}