using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.SqlServer {
    public class SqlServerStorage : SqlStorage {
        public SqlServerStorage(string nameOrConnectionString) : base(nameOrConnectionString) {}

        public SqlServerStorage(string nameOrConnectionString, SqlStorageOptions options)
            : base(nameOrConnectionString, options) {}

        protected override ISchemaBuilder GetSchemaBuilder() {
            return new SqlServerObjectsInstaller();
        }

        protected override IConnectionProvider GetConnectionProvider() {
            return new SqlStorageConnectionProvider(ConnectionString);
        }

        public override IMonitoringApi GetMonitoringApi() {
            return new SqlMonitoringApi(new SqlStorageConnectionProvider(ConnectionString), new SqlBook(), QueueProviders);
        }

        public override IStorageConnection GetConnection() {
            return new SqlStorageConnection(CreateAndOpenConnection(), 
                                     new SqlBook(), 
                                     new SqlServerDistributedLockAcquirer(), 
                                     QueueProviders);
        }
    }
}