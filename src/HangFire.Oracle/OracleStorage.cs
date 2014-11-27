using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.Oracle {
    public class OracleStorage : SqlStorage {

        public OracleStorage(string nameOrConnectionString) : base(nameOrConnectionString) {}
        public OracleStorage(string nameOrConnectionString, SqlStorageOptions options) : base(nameOrConnectionString, options) {}
        
        public override IMonitoringApi GetMonitoringApi() {
            return new OracleMonitoringApi(new OracleConnectionProvider(ConnectionString), SqlBook, QueueProviders);
        }

        protected override IConnectionProvider CreateConnectionProvider() {
            return new OracleConnectionProvider(ConnectionString);
        }

        protected override IPersistentJobQueueProvider GetDefaultPersistentJobQueueProvider() {
            return new OracleJobQueueProvider(ConnectionProvider, SqlBook, Options);
        }

        public override IStorageConnection GetConnection() {
            return new OracleStorageConnection(
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