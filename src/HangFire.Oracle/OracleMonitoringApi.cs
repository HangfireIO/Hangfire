using System;
using System.Data;
using System.Linq;
using Dapper;
using Hangfire.Sql;
using Hangfire.States;
using Hangfire.Storage.Monitoring;
using Oracle.ManagedDataAccess.Client;

namespace Hangfire.Oracle {
    public class OracleMonitoringApi : SqlMonitoringApi {
        public OracleMonitoringApi(IConnectionProvider connectionProvider, SqlBook sqlBook, PersistentJobQueueProviderCollection queueProviders) : base(connectionProvider, sqlBook, queueProviders) {}

        public override StatisticsDto GetStatistics() {
            return UseConnection(connection => {
                var sqls = SqlBook.SqlMonitoringApi_GetStatistics.Split(';');

                var countByStates = connection.Query(sqls[0]).ToDictionary(x => x.State, x => x.Count);
                Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;
                var stats = new StatisticsDto();
                stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                stats.Failed = getCountIfExists(FailedState.StateName);
                stats.Processing = getCountIfExists(ProcessingState.StateName);
                stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                var xx = connection.Query(sqls[1]);

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
    }
}