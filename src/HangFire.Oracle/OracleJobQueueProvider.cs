using System.Data;
using Hangfire.Sql;

namespace Hangfire.Oracle {
    public class OracleJobQueueProvider : SqlJobQueueProvider {
        public OracleJobQueueProvider(SqlBook sqkBook, SqlStorageOptions options) : base(sqkBook, options) {}

        public override IPersistentJobQueue GetJobQueue(IDbConnection connection) {
            return new OracleJobQueue(connection, SqkBook, Options);
        }
    }
}