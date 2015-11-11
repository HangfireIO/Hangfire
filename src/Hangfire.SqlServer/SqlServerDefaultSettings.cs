namespace Hangfire.SqlServer
{
    public class SqlServerDefaultSettings : ISqlServerSettings
    {
        public string TransformScript(string script)
        {
            return script;
        }

        public string CountersAggregationQuery { get { return null; } }
        public string SetJobParameterSql { get { return null; } }
    }
}