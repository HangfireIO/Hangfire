using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.SqlServer {
    public class SqlServerStorage : SqlStorage {
        public SqlServerStorage(string nameOrConnectionString) : base(nameOrConnectionString) { }

        public SqlServerStorage(string nameOrConnectionString, SqlStorageOptions options)
            : base(nameOrConnectionString, options) {}

        protected override SqlBook CreateSqlBook() {
            return new SqlBook();
        }

        protected override ISchemaBuilder GetSchemaBuilder() {
            return new SqlServerObjectsInstaller();
        }

        protected override IConnectionProvider GetConnectionProvider() {
            return new SqlStorageConnectionProvider(ConnectionString);
        }

        public override IMonitoringApi GetMonitoringApi() {
            return new SqlMonitoringApi(new SqlStorageConnectionProvider(ConnectionString), SqlBook, QueueProviders);
        }

        public override IStorageConnection GetConnection() {
            return new SqlStorageConnection(CreateAndOpenConnection(),
                                     SqlBook, 
                                     new SqlServerDistributedLockAcquirer(), 
                                     QueueProviders);
        }
    }
}