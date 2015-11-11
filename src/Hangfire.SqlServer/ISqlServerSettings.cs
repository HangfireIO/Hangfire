namespace Hangfire.SqlServer
{
    public interface ISqlServerSettings
    {
        string TransformScript(string script);
        string CountersAggregationSql { get; }
        string SetJobParameterSql { get; }
        string SetRangeInHashSql { get; }
        string AddToSetSql { get; }
    }
}