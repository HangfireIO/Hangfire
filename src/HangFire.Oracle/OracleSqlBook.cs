using System.Reflection;
using Hangfire.Sql;

namespace Hangfire.Oracle {
    public class OracleSqlBook : SqlBook {
        public OracleSqlBook() {
            FixSqls();

            ProcessedTables = new[] {
                "Counter",
                "Job",
                "ListTable",
                "SetTable",
                "HashTable",
            };

            SqlConnection_CreateExpiredJob_Job = @"
insert into Job (InvocationData, Arguments, CreatedAt, ExpireAt)
values (:invocationData, :arguments, :createdAt, :expireAt)
returning Id into :pid";

            SqlWriteOnlyTransaction_AddToSet = @"
merge into SetTable Target
using (select :key Key, :value Value, :score Score from dual) Source
on (Target.Key = Source.Key and Target.Value = Source.Value)
when matched then update set Score = Source.Score
when not matched then insert (Key, Value, Score) values (Source.Key, Source.Value, Source.Score)";
            SqlWriteOnlyTransaction_SetRangeInHash = @"
merge into Hashtable Target
using (select :key Key, :field Field, :value Value from dual) Source 
on (Target.Key = Source.Key and Target.Field = Source.Field)
when matched then update set Value = Source.Value
when not matched then insert (Key, Field, Value) values (Source.Key, Source.Field, Source.Value)
";
            SqlWriteOnlyTransaction_SetJobState = @"
insert into State (JobId, Name, Reason, CreatedAt, Data)
values (:jobId, :name, :reason, :createdAt, :data) returning Id into :pId;
update Job set StateId = :stateId, StateName = :name where Id = :pid;
";

            SqlConnection_AnnounceServer = @"
merge into Server Target
using (select :id Id, :data Data, :heartbeat Heartbeat from dual) Source
on (Target.Id = Source.Id )
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat 
when not matched then insert (Id, Data, LastHeartbeat) values (Source.Id, Source.Data, Source.Heartbeat)
";

            SqlConnection_GetFirstByLowestScoreFromSet =
@"select Value from 
(select Value from SetTable where Key = :key and Score between :pfrom and :pto order by Score)
where rownum <= 1";

            SqlJobQueue_Dequeue_fetched_null = @"
update JobQueue j
   set FetchedAt = sys_extract_utc(systimestamp)
 where Id in (select Id
                from (select Id
                        from JobQueue
                       where FetchedAt is null
                         and Queue in :queues
                       order by queue, fetchedAt)
               where rownum <= 1)
returning Id, JobId, Queue into :id, :jobid, :queue";

            SqlJobQueue_Dequeue_fetched_before = @"
update JobQueue j
   set FetchedAt = sys_extract_utc(systimestamp)
 where Id in (select Id
                from (select Id
                        from JobQueue
                       where FetchedAt < sys_extract_utc(systimestamp) + 1/24/60/(60 + :timeout)
                         and Queue in (' ')
                       order by queue, fetchedAt)
               where rownum <= 1)
returning Id, JobId, Queue into :id, :jobid, :queue";

            ExpirationManager_Execute = "delete from {0} where ExpireAt < :now";

        }

        private void FixSqls() {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var fieldInfo in fields) {
                if (fieldInfo.FieldType != typeof(string)) {
                    continue;
                }
                fieldInfo.SetValue(this, FromSqlServerToOracle(fieldInfo.GetValue(this).ToString()));
            }
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