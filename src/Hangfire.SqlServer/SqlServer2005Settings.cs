using System.Text.RegularExpressions;

namespace Hangfire.SqlServer
{
    public class SqlServer2005Settings : ISqlServerSettings
    {
        public string TransformScript(string script)
        {
            return Regex.Replace(script, @"\[datetime2\]\([0-9]\)", "[datetime]");
        }
    }
}