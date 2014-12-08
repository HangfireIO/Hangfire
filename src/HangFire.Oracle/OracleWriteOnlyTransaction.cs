// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Data;
using Dapper;
using Hangfire.Common;
using Hangfire.Sql;
using Hangfire.States;

namespace Hangfire.Oracle
{
    public class OracleWriteOnlyTransaction : SqlWriteOnlyTransaction
    {
        public OracleWriteOnlyTransaction(IDbConnection connection, SqlBook sqlBook,
            PersistentJobQueueProviderCollection queueProviders) : base(connection, sqlBook, queueProviders) {}

        public override void SetJobState(string jobId, IState state)
        {
            var sqls = SqlBook.SqlWriteOnlyTransaction_SetJobState.Split(';');
            var param = new DynamicParameters();
            param.Add("jobId", jobId);
            param.Add("name", state.Name);
            param.Add("reason", state.Reason);
            param.Add("createdAt", DateTime.UtcNow);
            param.Add("data", JobHelper.ToJson(state.SerializeData()));
            param.Add("pid", dbType: DbType.Int32, direction: ParameterDirection.Output);

            QueueCommand((c, t) =>
            {
                c.Execute(sqls[0], param, t);
                var stateId = param.Get<int>("pid");

                param = new DynamicParameters();
                param.Add("stateId", stateId);
                param.Add("name", state.Name);
                param.Add("pid", jobId);
                c.Execute(sqls[1], param, t);
            });
        }
    }
}