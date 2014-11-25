using System.Reflection;
using Hangfire.Sql;

namespace Hangfire.Oracle {
    public class OracleSqlBook : SqlBook {
        public OracleSqlBook() {
            FixSqls();

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
            SqlConnection_AnnounceServer = @"
merge into Server Target
using (select :id Id, :data Data, :heartbeat Heartbeat from dual) Source
on (Target.Id = Source.Id )
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat 
when not matched then insert (Id, Data, LastHeartbeat) values (Source.Id, Source.Data, Source.Heartbeat)
";
            ExpirationManager_Execute = "delete from {0} where ExpireAt < :now";

        }

        private void FixSqls() {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var fieldInfo in fields) {
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