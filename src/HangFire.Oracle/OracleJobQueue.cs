using System.Data;
using System.Linq;
using Dapper;
using Hangfire.Sql;

namespace Hangfire.Oracle {
    public class OracleJobQueue : SqlJobQueue {
        public OracleJobQueue(IDbConnection connection, SqlBook sqlBook, SqlStorageOptions options)
            : base(connection, sqlBook, options) {}

        protected override FetchedJob QueryFetchedJob(string[] queues, string sql) {
            var param = new DynamicParameters();
            param.Add("queues", queues);
            param.Add("timeout", Options.InvisibilityTimeout.Negate().TotalSeconds);
            param.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("jobid", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("queue", dbType: DbType.String, direction: ParameterDirection.Output);

            return Connection.Query<FetchedJob>(
                sql,
                param)
                .SingleOrDefault();
        }
    }
}