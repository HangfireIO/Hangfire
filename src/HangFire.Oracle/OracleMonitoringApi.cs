using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Hangfire.Common;
using Hangfire.Sql;
using Hangfire.Sql.Entities;
using Hangfire.States;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Oracle {
    public class OracleMonitoringApi : SqlMonitoringApi {
        public OracleMonitoringApi(IConnectionProvider connectionProvider, SqlBook sqlBook, PersistentJobQueueProviderCollection queueProviders) : base(connectionProvider, sqlBook, queueProviders) {}

        public override StatisticsDto GetStatistics() {
            return UseConnection((connection,transaction) => {
                var sqls = SqlBook.SqlMonitoringApi_GetStatistics.Split(';');

                var rows = connection.Query(sqls[0], transaction).ToList();
                var countByStates = new Dictionary<string, int>();
                foreach (var row in rows) {
                    countByStates.Add(row.STATE, Convert.ToInt32(row.COUNT));
                }
                Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;
                var stats = new StatisticsDto();
                stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                stats.Failed = getCountIfExists(FailedState.StateName);
                stats.Processing = getCountIfExists(ProcessingState.StateName);
                stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                stats.Servers = Convert.ToInt64(connection.Query(sqls[1], transaction).First().First() ?? 0);
                stats.Succeeded = Convert.ToInt64(connection.Query(sqls[2], transaction).First().First() ?? 0);
                stats.Deleted = Convert.ToInt64(connection.Query(sqls[3], transaction).First().First() ?? 0);

                stats.Recurring = Convert.ToInt64(connection.Query(sqls[4], transaction).First().First());
                stats.Queues = QueueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                    .Count();
                return stats;
            });
        }

        protected override List<SqlJob> GetEnqueuedJobs(IDbConnection connection, IDbTransaction transaction, IEnumerable<int> jobIds) {
            if (!jobIds.Any()) {
                return new List<SqlJob>();
            }
            var ids = String.Join(",", jobIds.Select(x => x.ToString()).ToArray());
            var sql = SqlBook.SqlMonitoringApi_EnqueuedJobs.Replace(":jobIds","("+ids+")");
            return connection.Query<SqlJob>(sql).ToList();
        }
    }
}