using System;
using System.Data;
using Dapper;
using Hangfire.Common;
using Hangfire.Sql;
using Hangfire.States;

namespace Hangfire.Oracle {
    public class OracleWriteOnlyTransaction : SqlWriteOnlyTransaction {
        public OracleWriteOnlyTransaction(IDbConnection connection, SqlBook sqlBook, PersistentJobQueueProviderCollection queueProviders) : base(connection, sqlBook, queueProviders) {}

        public override void SetJobState(string jobId, IState state) {
            var sqls = SqlBook.SqlWriteOnlyTransaction_SetJobState.Split(';');
            var param = new DynamicParameters();
            param.Add("jobId", jobId);
            param.Add("name", state.Name);
            param.Add("reason", state.Reason);
            param.Add("createdAt", DateTime.UtcNow);
            param.Add("data", JobHelper.ToJson(state.SerializeData()));
            param.Add("pid", dbType: DbType.Int32, direction: ParameterDirection.Output);

            QueueCommand(x => {
                x.Execute(sqls[0], param);
                var stateId = param.Get<int>("pid");

                param = new DynamicParameters();
                param.Add("stateId", stateId);
                param.Add("name", state.Name);
                param.Add("pid", jobId);
                x.Execute(sqls[1], param);
            });
        }
    }
}