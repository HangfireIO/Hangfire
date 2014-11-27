using System.Data;
using Hangfire.Sql;

namespace Hangfire.Oracle {
    public class OracleJobQueueProvider : SqlJobQueueProvider {
        public OracleJobQueueProvider(IConnectionProvider connectionProvider, SqlBook sqkBook, SqlStorageOptions options) : base(connectionProvider, sqkBook, options) {}

        public override IPersistentJobQueue GetJobQueue() {
            return new OracleJobQueue(ConnectionProvider, SqkBook, Options);
        }
    }
}