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
using Hangfire.Sql;

namespace Hangfire.Oracle
{
    public class OracleJobQueue : SqlJobQueue
    {
        public OracleJobQueue(IConnectionProvider connectionProvider, SqlBook sqlBook, SqlStorageOptions options)
            : base(connectionProvider, sqlBook, options) {}

        private FetchedJob QueryFetchedJob(string[] queues, string sql, DynamicParameters param)
        {
            var strQueues = String.Join("','", queues);
            var finalSql = String.Format(sql, strQueues);
            int update = 0;
            using (var connection = ConnectionProvider.CreateAndOpenConnection())
            {
                update = connection.Execute(
                    finalSql,
                    param);
            }
            if (update == 0)
            {
                return null;
            }
            return new FetchedJob
            {
                Id = param.Get<int>("Id"),
                JobId = param.Get<int>("jobid"),
                Queue = param.Get<string>("queue")
            };
        }

        protected override FetchedJob QueryNeverFetchedJob(string[] queues)
        {
            var param = CreateOutputParameters();
            return QueryFetchedJob(queues, SqlBook.SqlJobQueue_Dequeue_fetched_null, param);
        }

        protected override FetchedJob QueryFetchedAtJob(string[] queues)
        {
            var param = CreateOutputParameters();
            param.Add("timeout", Options.InvisibilityTimeout.Negate().TotalSeconds);
            return QueryFetchedJob(queues, SqlBook.SqlJobQueue_Dequeue_fetched_before, param);
        }

        private static DynamicParameters CreateOutputParameters()
        {
            var param = new DynamicParameters();
            param.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("jobid", dbType: DbType.Int32, direction: ParameterDirection.Output);
            param.Add("queue", dbType: DbType.String, direction: ParameterDirection.Output);
            return param;
        }
    }
}