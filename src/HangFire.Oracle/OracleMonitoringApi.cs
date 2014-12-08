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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Hangfire.Common;
using Hangfire.Sql;
using Hangfire.Sql.Entities;
using Hangfire.States;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Oracle
{
    public class OracleMonitoringApi : SqlMonitoringApi
    {
        public OracleMonitoringApi(IConnectionProvider connectionProvider, SqlBook sqlBook,
            PersistentJobQueueProviderCollection queueProviders) : base(connectionProvider, sqlBook, queueProviders) {}

        public override StatisticsDto GetStatistics()
        {
            return UseConnection((connection, transaction) =>
            {
                var sqls = SqlBook.SqlMonitoringApi_GetStatistics.Split(';');

                var rows = connection.Query(sqls[0], transaction).ToList();
                var countByStates = new Dictionary<string, int>();
                foreach (var row in rows)
                {
                    countByStates.Add(row.STATE, Convert.ToInt32(row.COUNT));
                }
                Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;
                var stats = new StatisticsDto();
                stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                stats.Failed = getCountIfExists(FailedState.StateName);
                stats.Processing = getCountIfExists(ProcessingState.StateName);
                stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                stats.Servers = Convert.ToInt64(connection.Query<int>(sqls[1], transaction: transaction).Single());
                stats.Succeeded = Convert.ToInt64(connection.Query<int>(sqls[2], transaction: transaction).Single());
                stats.Deleted =
                    Convert.ToInt64(connection.Query<int?>(sqls[3], transaction: transaction).SingleOrDefault());
                stats.Recurring = Convert.ToInt64(connection.Query<int>(sqls[4], transaction: transaction).Single());

                stats.Queues = QueueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                    .Count();
                return stats;
            });
        }

        protected override List<SqlJob> GetEnqueuedJobs(IDbConnection connection, IDbTransaction transaction,
            IEnumerable<int> jobIds)
        {
            if (!jobIds.Any())
            {
                return new List<SqlJob>();
            }
            var ids = String.Join(",", jobIds.Select(x => x.ToString()).ToArray());
            var sql = SqlBook.SqlMonitoringApi_EnqueuedJobs.Replace(":jobIds", "(" + ids + ")");
            return connection.Query<SqlJob>(sql).ToList();
        }

        public override JobDetailsDto JobDetails(string jobId)
        {
            var sqls = SqlBook.SqlMonitoringApi_JobDetails.Split(';');

            return UseConnection((connection, transaction) =>
            {
                var job = connection.Query<SqlJob>(sqls[0], new {id = jobId}, transaction).SingleOrDefault();
                if (job == null)
                {
                    return null;
                }

                var parameters = connection.Query<JobParameter>(sqls[1], new {id = jobId}, transaction)
                    .ToList()
                    .ToDictionary(x => x.Name, x => x.Value);

                var history = connection.Query<SqlState>(sqls[2], new {id = jobId}, transaction)
                    .ToList()
                    .Select(x => new StateHistoryDto
                    {
                        StateName = x.Name,
                        CreatedAt = x.CreatedAt,
                        Reason = x.Reason,
                        Data = JobHelper.FromJson<Dictionary<string, string>>(x.Data)
                    })
                    .ToList();
                return new JobDetailsDto
                {
                    CreatedAt = job.CreatedAt,
                    Job = DeserializeJob(job.InvocationData, job.Arguments),
                    History = history,
                    Properties = parameters
                };
            });
        }

        protected override Dictionary<string, long> ExecuteHourlyTimelineStatsQuery(IDbConnection connection,
            IDbTransaction transaction, List<string> keys)
        {
            return GetStats(connection, transaction, SqlBook.SqlMonitoringApi_GetHourlyTimelineStats, keys);
        }

        protected override Dictionary<string, long> ExecuteGetTimelineStatsQuery(IDbConnection connection,
            IDbTransaction transaction, List<string> keys)
        {
            return GetStats(connection, transaction, SqlBook.SqlMonitoringApi_GetTimelineStats, keys);
        }

        private Dictionary<string, long> GetStats(IDbConnection connection, IDbTransaction transaction, string sql,
            List<string> keys)
        {
            var ids = " ('" + String.Join("','", keys.ToArray()) + "') ";
            string finalSql = sql.Replace(":keys", ids);
            return connection.Query(finalSql, transaction: transaction)
                .ToDictionary<dynamic, string, long>(item => item.KEY, item => Convert.ToInt64(item.COUNT));
        }
    }
}