namespace Hangfire.SqlServer
{
    public class SqlServerDefaultSettings : ISqlServerSettings
    {
        public string TransformScript(string script)
        {
            return script;
        }
    }
}