namespace Hangfire.SqlServer
{
    public interface ISqlServerSettings
    {
        string TransformScript(string script);
        string CountersAggregationSql { get; }
        string SetJobParameterSql { get; }
    }
}