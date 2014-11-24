using Hangfire.Sql;

namespace Hangfire.Oracle {
    public class OracleSqlBook : SqlBook {
        public OracleSqlBook() {
            SqlMonitoringApi_GetHourlyTimelineStats = FromSqlServerToOracle(SqlMonitoringApi_GetHourlyTimelineStats);
            SqlConnection_CreateExpiredJob_Job = FromSqlServerToOracle(SqlConnection_CreateExpiredJob_Job);
            SqlConnection_CreateExpiredJob_Parameter = FromSqlServerToOracle(SqlConnection_CreateExpiredJob_Parameter);
            SqlConnection_GetJobData = FromSqlServerToOracle(SqlConnection_GetJobData);
            SqlConnection_GetStateData = FromSqlServerToOracle(SqlConnection_GetStateData);
            SqlConnection_SetJobParameter = FromSqlServerToOracle(SqlConnection_SetJobParameter);
            SqlConnection_GetJobParameter = FromSqlServerToOracle(SqlConnection_GetJobParameter);
            SqlConnection_GetAllItemsFromSet = FromSqlServerToOracle(SqlConnection_GetAllItemsFromSet);
            SqlConnection_GetFirstByLowestScoreFromSet =
                FromSqlServerToOracle(SqlConnection_GetFirstByLowestScoreFromSet);
            SqlConnection_SetRangeInHash = FromSqlServerToOracle(SqlConnection_SetRangeInHash);
            SqlConnection_GetAllEntriesFromHash = FromSqlServerToOracle(SqlConnection_GetAllEntriesFromHash);
            SqlConnection_AnnounceServer = FromSqlServerToOracle(SqlConnection_AnnounceServer);
            SqlConnection_RemoveServer = FromSqlServerToOracle(SqlConnection_RemoveServer);
            SqlConnection_Heartbeat = FromSqlServerToOracle(SqlConnection_Heartbeat);
            SqlConnection_RemoveTimedOutServers = FromSqlServerToOracle(SqlConnection_RemoveTimedOutServers);
            SqlMonitoringApi_Servers = FromSqlServerToOracle(SqlMonitoringApi_Servers);
            SqlMonitoringApi_JobDetails = FromSqlServerToOracle(SqlMonitoringApi_JobDetails);
            SqlMonitoringApi_GetStatistics = @"
BEGIN 
open :res1 for select StateName as State, count(Id) as Count From Job group by StateName having StateName is not null;
open :res2 for select count(Id) from Server;
open :res3 for select sum(Value) from Counter where Key = 'stats:succeeded';
open :res4 for select sum(Value) from Counter where Key = 'stats:deleted';
open :res5 for select count(*) from SetTable where Key = 'recurring-jobs';
END;
";
            SqlMonitoringApi_GetHourlyTimelineStats = FromSqlServerToOracle(SqlMonitoringApi_GetHourlyTimelineStats);
            SqlMonitoringApi_GetTimelineStats = FromSqlServerToOracle(SqlMonitoringApi_GetTimelineStats);
            SqlMonitoringApi_EnqueuedJobs = FromSqlServerToOracle(SqlMonitoringApi_EnqueuedJobs);
            SqlMonitoringApi_GetNumberOfJobsByStateName =
                FromSqlServerToOracle(SqlMonitoringApi_GetNumberOfJobsByStateName);
            SqlMonitoringApi_GetJobs = FromSqlServerToOracle(SqlMonitoringApi_GetJobs);
            SqlMonitoringApi_FetchedJobs = FromSqlServerToOracle(SqlMonitoringApi_FetchedJobs);

            SqlWriteOnlyTransaction_ExpireJob = FromSqlServerToOracle(SqlWriteOnlyTransaction_ExpireJob);
            SqlWriteOnlyTransaction_SetJobState = FromSqlServerToOracle(SqlWriteOnlyTransaction_SetJobState);
            SqlWriteOnlyTransaction_AddJobState = FromSqlServerToOracle(SqlWriteOnlyTransaction_AddJobState);
            SqlWriteOnlyTransaction_IncrementCounter = FromSqlServerToOracle(SqlWriteOnlyTransaction_IncrementCounter);
            SqlWriteOnlyTransaction_IncrementCounter_expirein =
                FromSqlServerToOracle(SqlWriteOnlyTransaction_IncrementCounter_expirein);
            SqlWriteOnlyTransaction_DecrementCounter = FromSqlServerToOracle(SqlWriteOnlyTransaction_DecrementCounter);
            SqlWriteOnlyTransaction_DecrementCounter_expirein =
                FromSqlServerToOracle(SqlWriteOnlyTransaction_DecrementCounter_expirein);
            SqlWriteOnlyTransaction_AddToSet = @"
merge into SetTable Target
using (select :key Key, :value Value, :score Score from dual) Source
on (Target.Key = Source.Key and Target.Value = Source.Value)
when matched then update set Score = Source.Score
when not matched then insert (Key, Value, Score) values (Source.Key, Source.Value, Source.Score)";
            SqlWriteOnlyTransaction_RemoveFromSet = FromSqlServerToOracle(SqlWriteOnlyTransaction_RemoveFromSet);
            SqlWriteOnlyTransaction_InsertToList = FromSqlServerToOracle(SqlWriteOnlyTransaction_InsertToList);
            SqlWriteOnlyTransaction_RemoveFromList = FromSqlServerToOracle(SqlWriteOnlyTransaction_RemoveFromList);
            SqlWriteOnlyTransaction_TrimList = FromSqlServerToOracle(SqlWriteOnlyTransaction_TrimList);
            SqlWriteOnlyTransaction_SetRangeInHash = @"
merge into Hashtable Target
using (select :key Key, :field Field, :value Value from dual) Source 
on (Target.Key = Source.Key and Target.Field = Source.Field)
when matched then update set Value = Source.Value
when not matched then insert (Key, Field, Value) values (Source.Key, Source.Field, Source.Value)
";
            SqlWriteOnlyTransaction_RemoveHash = FromSqlServerToOracle(SqlWriteOnlyTransaction_RemoveHash);
        }

        private string FromSqlServerToOracle(string sql) {
            return sql.Remove("[", "]")
                      .Replace("HangFire.Set", "SetTable")
                      .Replace("HangFire.List", "ListTable")
                      .Replace("HangFire.Hash", "HashTable")
                      .Replace("@", ":")
                      .Replace("= N'", "= '")
                      .Replace(" as ", " ")
                      .Remove("HangFire.")
                      ;
        }
    }

    public static class StringExt {
        public static string Remove(this string str, params string[] parts) {
            foreach (var part in parts) {
                str = str.Replace(part, "");
            }
            return str;
        }
    }
}