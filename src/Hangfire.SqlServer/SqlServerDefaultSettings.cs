namespace Hangfire.SqlServer
{
    public class SqlServerDefaultSettings : ISqlServerSettings
    {
        public string TransformScript(string script)
        {
            return script;
        }

        public string CountersAggregationSql { get { return null; } }
        public string SetJobParameterSql { get { return null; } }
        public string SetRangeInHashSql { get { return null; } }
        public string AddToSetSql { get { return null; } }
        public string SetRangeInHashWriteOnlySql { get { return null; } }
        public string AnnounceServerSql { get { return null; } }
    }
}