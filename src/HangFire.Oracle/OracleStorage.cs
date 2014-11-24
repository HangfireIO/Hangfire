using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.Oracle {
    public class OracleStorage : SqlStorage {

        public OracleStorage(string nameOrConnectionString) : base(nameOrConnectionString) {}
        public OracleStorage(string nameOrConnectionString, SqlStorageOptions options) : base(nameOrConnectionString, options) {}
        
        public override IMonitoringApi GetMonitoringApi() {
            return new OracleMonitoringApi(new OracleConnectionProvider(ConnectionString), SqlBook, QueueProviders);
        }

        protected override IConnectionProvider GetConnectionProvider() {
            return new OracleConnectionProvider(ConnectionString);
        }

        public override IStorageConnection GetConnection() {
            return new SqlStorageConnection(
                CreateAndOpenConnection(), 
                SqlBook, 
                new OracleDistributedLockAcquirer(),
                QueueProviders);
        }

        protected override SqlBook CreateSqlBook() {
            return new OracleSqlBook();
        }

        protected override ISchemaBuilder GetSchemaBuilder() {
            return new OracleShemaBuilder(ConnectionString);
        }
    }
}