// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using Hangfire.Sql.Properties;
using Hangfire.Storage;

namespace Hangfire.Sql
{
    public class SqlJobQueue : IPersistentJobQueue
    {
        private readonly SqlStorageOptions _options;
        private readonly IDbConnection _connection;

        public SqlJobQueue(IDbConnection connection, SqlStorageOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (connection == null) throw new ArgumentNullException("connection");

            _options = options;
            _connection = connection;
        }

        [NotNull]
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            FetchedJob fetchedJob;

            const string fetchJobSqlTemplate = @"
set transaction isolation level read committed
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.Id, INSERTED.JobId, INSERTED.Queue
where FetchedAt {0}
and Queue in @queues";

            // Sql query is splitted to force SQL Server to use 
            // INDEX SEEK instead of INDEX SCAN operator.
            var fetchConditions = new[] { "is null", "< DATEADD(second, @timeout, GETUTCDATE())" };
            var currentQueryIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                fetchedJob = _connection.Query<FetchedJob>(
                    String.Format(fetchJobSqlTemplate, fetchConditions[currentQueryIndex]),
                    new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds })
                    .SingleOrDefault();

                if (fetchedJob == null)
                {
                    if (currentQueryIndex == fetchConditions.Length - 1)
                    {
                        cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                currentQueryIndex = (currentQueryIndex + 1) % fetchConditions.Length;
            } while (fetchedJob == null);

            return new SqlFetchedJob(
                _connection,
                fetchedJob.Id,
                fetchedJob.JobId.ToString(CultureInfo.InvariantCulture),
                fetchedJob.Queue);
        }

        public void Enqueue(string queue, string jobId)
        {
            const string enqueueJobSql = @"
insert into HangFire.JobQueue (JobId, Queue) values (@jobId, @queue)";

            _connection.Execute(enqueueJobSql, new { jobId = jobId, queue = queue });
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private class FetchedJob
        {
            public int Id { get; set; }
            public int JobId { get; set; }
            public string Queue { get; set; }
        }
    }
}