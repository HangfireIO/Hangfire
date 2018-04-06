// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using Dapper;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.SqlServer
{
#pragma warning disable 618
    internal class CountersAggregator : IServerComponent
#pragma warning restore 618
    {
        private static readonly ILog Logger = LogProvider.For<CountersAggregator>();
        
        // This number should be high enough to aggregate counters efficiently,
        // but low enough to not to cause large amount of row locks to be taken.
        // Lock escalation to page locks may pause the background processing.
        private const int NumberOfRecordsInSinglePass = 1000;
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromMilliseconds(500);

        private readonly SqlServerStorage _storage;
        private readonly TimeSpan _interval;

        public CountersAggregator(SqlServerStorage storage, TimeSpan interval)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            _storage = storage;
            _interval = interval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.Debug("Aggregating records in 'Counter' table...");

            int removedCount = 0;

            do
            {
                _storage.UseConnection(null, connection =>
                {
                    removedCount = connection.Execute(
                        GetAggregationQuery(_storage),
                        new { now = DateTime.UtcNow, count = NumberOfRecordsInSinglePass },
                        commandTimeout: 0);
                });

                if (removedCount >= NumberOfRecordsInSinglePass)
                {
                    cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            } while (removedCount >= NumberOfRecordsInSinglePass);

            Logger.Trace("Records from the 'Counter' table aggregated.");

            cancellationToken.WaitHandle.WaitOne(_interval);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }

        private static string GetAggregationQuery(SqlServerStorage storage)
        {
            return 
$@"DECLARE @RecordsToAggregate TABLE
(
	[Key] NVARCHAR(100) NOT NULL,
	[Value] SMALLINT NOT NULL,
	[ExpireAt] DATETIME NULL
)

SET XACT_ABORT ON
SET TRANSACTION ISOLATION LEVEL READ COMMITTED
BEGIN TRAN

DELETE TOP (@count) C
OUTPUT DELETED.[Key], DELETED.[Value], DELETED.[ExpireAt] INTO @RecordsToAggregate
FROM [{storage.SchemaName}].[Counter] C WITH (READPAST, XLOCK, INDEX(0))

SET NOCOUNT ON

;MERGE [{storage.SchemaName}].[AggregatedCounter] WITH (HOLDLOCK) AS [Target]
USING (
	SELECT [Key], SUM([Value]) as [Value], MAX([ExpireAt]) AS [ExpireAt] FROM @RecordsToAggregate
	GROUP BY [Key]) AS [Source] ([Key], [Value], [ExpireAt])
ON [Target].[Key] = [Source].[Key]
WHEN MATCHED THEN UPDATE SET 
	[Target].[Value] = [Target].[Value] + [Source].[Value],
	[Target].[ExpireAt] = (SELECT MAX([ExpireAt]) FROM (VALUES ([Source].ExpireAt), ([Target].[ExpireAt])) AS MaxExpireAt([ExpireAt]))
WHEN NOT MATCHED THEN INSERT ([Key], [Value], [ExpireAt]) VALUES ([Source].[Key], [Source].[Value], [Source].[ExpireAt]);

COMMIT TRAN";
        }
    }
}

