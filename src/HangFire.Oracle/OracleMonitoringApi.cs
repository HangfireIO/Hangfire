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
                var dynParams = new OracleDynamicParameters();
                dynParams.Add(":res1", OracleDbType.RefCursor, ParameterDirection.Output);
                dynParams.Add(":res2", OracleDbType.RefCursor, ParameterDirection.Output);
                dynParams.Add(":res3", OracleDbType.RefCursor, ParameterDirection.Output);
                dynParams.Add(":res4", OracleDbType.RefCursor, ParameterDirection.Output);
                dynParams.Add(":res5", OracleDbType.RefCursor, ParameterDirection.Output);
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(SqlBook.SqlMonitoringApi_GetStatistics, dynParams)) {
                    

                    var countByStates = multi.Read().ToDictionary(x => x.State, x => x.Count);
                    Func<string, int> getCountIfExists = name => countByStates.ContainsKey(name) ? countByStates[name] : 0;

                    stats.Enqueued = getCountIfExists(EnqueuedState.StateName);
                    stats.Failed = getCountIfExists(FailedState.StateName);
                    stats.Processing = getCountIfExists(ProcessingState.StateName);
                    stats.Scheduled = getCountIfExists(ScheduledState.StateName);

                    stats.Servers = Convert.ToInt64(multi.Read<decimal>().SingleOrDefault());
                    stats.Succeeded = multi.Read<int?>().SingleOrDefault() ?? 0;
                    stats.Deleted = multi.Read<int?>().SingleOrDefault() ?? 0;

                    stats.Recurring = Convert.ToInt64(multi.Read<decimal>().SingleOrDefault());
                }

                stats.Queues = QueueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi(connection).GetQueues())
                    .Count();

                return stats;
            });
        }
    }
}