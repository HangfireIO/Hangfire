namespace Hangfire.SqlServer
{
    public interface ISqlServerSettings
    {
        string TransformScript(string script);
        string CountersAggregationQuery { get; }
    }
}