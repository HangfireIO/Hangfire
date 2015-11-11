using System.Text.RegularExpressions;

namespace Hangfire.SqlServer
{
    public class SqlServer2005Settings : ISqlServerSettings
    {
        public string TransformScript(string script)
        {
            return Regex.Replace(script, @"\[datetime2\]\([0-9]\)", "[datetime]");
        }

        public string CountersAggregationQuery { get { return @"
DECLARE @RecordsToAggregate TABLE
(
	[Key] NVARCHAR(100) NOT NULL,
	[Value] SMALLINT NOT NULL,
	[ExpireAt] DATETIME NULL
)

SET TRANSACTION ISOLATION LEVEL READ COMMITTED
BEGIN TRAN

DELETE TOP (@count) [{0}].[Counter] with (readpast)
OUTPUT DELETED.[Key], DELETED.[Value], DELETED.[ExpireAt] INTO @RecordsToAggregate

SET NOCOUNT ON

UPDATE [{0}].[AggregatedCounter]
SET 
	[Value] = ac.[Value] + ra.[Value],
	[ExpireAt] = (SELECT MAX([ExpireAt]) FROM (VALUES (ac.ExpireAt), (ra.[ExpireAt])) AS MaxExpireAt([ExpireAt]))
FROM [{0}].[AggregatedCounter] AS ac
JOIN @RecordsToAggregate ra
ON ac.[Key] = ra.[Key];

INSERT INTO [{0}].[AggregatedCounter]
SELECT [Key], SUM([Value]) as [Value], MAX([ExpireAt]) AS [ExpireAt] 
FROM @RecordsToAggregate 
GROUP BY [Key]
HAVING [Key] NOT IN (SELECT [Key] FROM [{0}].[AggregatedCounter]);

COMMIT TRAN"; } }
    }
}