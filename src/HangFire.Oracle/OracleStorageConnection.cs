using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Hangfire.Common;
using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.Oracle {
    public class OracleStorageConnection : SqlStorageConnection {
        public OracleStorageConnection(IDbConnection connection, SqlBook sqlBook, IDistributedLockAcquirer distributedLockAcquirer, PersistentJobQueueProviderCollection queueProviders) : base(connection, sqlBook, distributedLockAcquirer, queueProviders) {}

        public override IWriteOnlyTransaction CreateWriteTransaction() {
            return new OracleWriteOnlyTransaction(Connection, SqlBook, QueueProviders);
        }

        public override string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn) {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");

            var invocationData = InvocationData.Serialize(job);
            var param = new DynamicParameters();
            param.Add("invocationData", JobHelper.ToJson(invocationData));
            param.Add("arguments", invocationData.Arguments);
            param.Add("createdAt", createdAt);
            param.Add("expireAt", createdAt.Add(expireIn));
            param.Add("pid", dbType: DbType.Int32, direction: ParameterDirection.Output);

            Connection.Execute(
                SqlBook.SqlConnection_CreateExpiredJob_Job,
                param);
            var jobId = param.Get<int>("pid").ToString();

            if (parameters.Count > 0) {
                var parameterArray = new object[parameters.Count];
                int parameterIndex = 0;
                foreach (var parameter in parameters) {
                    parameterArray[parameterIndex++] = new {
                        jobId = jobId,
                        name = parameter.Key,
                        value = parameter.Value
                    };
                }
                Connection.Execute(SqlBook.SqlConnection_CreateExpiredJob_Parameter, parameterArray);
            }

            return jobId;
        }
    }
}