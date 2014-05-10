// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    internal class SqlServerJobQueue : IPersistentJobQueue
    {
        private readonly SqlServerStorageOptions _options;
        private readonly IDbConnection _connection;

        public SqlServerJobQueue(SqlServerStorageOptions options, IDbConnection connection)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (connection == null) throw new ArgumentNullException("connection");

            _options = options;
            _connection = connection;
        }

        public IProcessingJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            dynamic idAndQueue;

            const string fetchJobSqlTemplate = @"
set transaction isolation level read committed
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.JobId, INSERTED.Queue
where FetchedAt {0}
and Queue in @queues";

            // Sql query is splitted to force SQL Server to use 
            // INDEX SEEK instead of INDEX SCAN operator.
            var fetchConditions = new[] { "is null", "< DATEADD(second, @timeout, GETUTCDATE())" };
            var currentQueryIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                idAndQueue = _connection.Query(
                    String.Format(fetchJobSqlTemplate, fetchConditions[currentQueryIndex]),
                    new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds })
                    .SingleOrDefault();

                if (idAndQueue == null)
                {
                    if (currentQueryIndex == fetchConditions.Length - 1)
                    {
                        cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                currentQueryIndex = (currentQueryIndex + 1) % fetchConditions.Length;
            } while (idAndQueue == null);

            return new SqlServerProcessingJob(
                _connection,
                idAndQueue.JobId.ToString(CultureInfo.InvariantCulture),
                idAndQueue.Queue);
        }

        public void AddToQueue(Queue<Action<SqlConnection>> actions, string queue, string jobId)
        {
            if (actions == null) throw new ArgumentNullException("actions");

            const string enqueueJobSql = @"
insert into HangFire.JobQueue (JobId, Queue) values (@jobId, @queue)";

            actions.Enqueue(x => x.Execute(
                enqueueJobSql, new { jobId = jobId, queue = queue }));
        }
    }
}