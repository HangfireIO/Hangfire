using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.Oracle {
    public class OracleStorage : SqlStorage {

        private static OracleSqlBook _sqlBook = new OracleSqlBook();

        public OracleStorage(string nameOrConnectionString) : base(nameOrConnectionString) {}
        public OracleStorage(string nameOrConnectionString, SqlStorageOptions options) : base(nameOrConnectionString, options) {}
        
        public override IMonitoringApi GetMonitoringApi() {
            return new OracleMonitoringApi(new OracleConnectionProvider(ConnectionString), _sqlBook, QueueProviders);
        }

        protected override IConnectionProvider GetConnectionProvider() {
            return new OracleConnectionProvider(ConnectionString);
        }

        public override IStorageConnection GetConnection() {
            return new SqlStorageConnection(
                CreateAndOpenConnection(), 
                _sqlBook, 
                new OracleDistributedLockAcquirer(),
                QueueProviders);
        }

        protected override ISchemaBuilder GetSchemaBuilder() {
            return new OracleShemaBuilder(ConnectionString);
        }
    }
}