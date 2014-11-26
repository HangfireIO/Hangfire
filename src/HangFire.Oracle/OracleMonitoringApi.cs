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
using Oracle.ManagedDataAccess.Client;

namespace Hangfire.Oracle {
    public class OracleMonitoringApi : SqlMonitoringApi {
        public OracleMonitoringApi(IConnectionProvider connectionProvider, SqlBook sqlBook, PersistentJobQueueProviderCollection queueProviders) : base(connectionProvider, sqlBook, queueProviders) {}

        public override StatisticsDto GetStatistics() {
            return UseConnection(connection => {
                var sqls = SqlBook.SqlMonitoringApi_GetStatistics.Split(';');

                var rows = connection.Query(sqls[0]).ToList();
                var dic = new Dictionary<string, int>();
                foreach (var row in rows) {
                    dic.Add(row.STATE, Convert.ToInt32(row.COUNT));
                }
                var countByStates = dic; //connection.Query(sqls[0]).ToDictionary(x => x.State.ToString(), x => x.Count.ToString());
                Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;
                var stats = new StatisticsDto();
                stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                stats.Failed = getCountIfExists(FailedState.StateName);
                stats.Processing = getCountIfExists(ProcessingState.StateName);
                stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                stats.Servers = Convert.ToInt64(connection.Query(sqls[1]).First().First() ?? 0);
                stats.Succeeded = Convert.ToInt64(connection.Query(sqls[2]).First().First() ?? 0);
                stats.Deleted = Convert.ToInt64(connection.Query(sqls[3]).First().First() ?? 0);

                stats.Recurring = Convert.ToInt64(connection.Query(sqls[3]).First().First());
                stats.Queues = QueueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi(connection).GetQueues())
                    .Count();
                return stats;
            });
        }

        protected override List<SqlJob> GetEnqueuedJobs(IDbConnection connection, IEnumerable<int> jobIds) {
            if (!jobIds.Any()) {
                return new List<SqlJob>();
            }
            var ids = String.Join(",", jobIds.Select(x => x.ToString()).ToArray());
            var sql = SqlBook.SqlMonitoringApi_EnqueuedJobs.Replace(":jobIds","("+ids+")");
            return connection.Query<SqlJob>(sql).ToList();
        }
    }
}