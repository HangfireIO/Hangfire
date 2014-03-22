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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using HangFire.Common;
using HangFire.Server;
using HangFire.SqlServer.Entities;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    internal class SqlServerFetcher : IJobFetcher
    {
        private static readonly TimeSpan JobTimeOut = TimeSpan.FromMinutes(30);

        private readonly IDbConnection _connection;
        private readonly string[] _queues;

        public SqlServerFetcher(IDbConnection connection, string[] queues)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            _connection = connection;
            _queues = queues;
        }

        public JobPayload FetchNextJob(CancellationToken cancellationToken)
        {
            SqlJob job = null;
            string queueName = null;

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

                var idAndQueue = _connection.Query(
                    String.Format(fetchJobSqlTemplate, fetchConditions[currentQueryIndex]),
                    new { queues = _queues, timeout = JobTimeOut.Negate().TotalSeconds })
                    .SingleOrDefault();

                if (idAndQueue != null)
                {
                    // Using DynamicParameters with explicit parameter type 
                    // instead of anonymous object, because of a strange
                    // behavior of a query plan builder: execution plan
                    // was based on index scan instead of index seek. 
                    // As a result, this query was the slowest.
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", idAndQueue.JobId, dbType: DbType.Int32);

                    job = _connection.Query<SqlJob>(
                        @"select Id, InvocationData, Arguments from HangFire.Job where Id = @id",
                        parameters)
                        .SingleOrDefault();

                    if (job == null)
                    {
                        _connection.Execute(
                            @"delete from HangFire.JobQueue where JobId = @id",
                            parameters);
                    }

                    queueName = idAndQueue.Queue;
                }

                if (job == null && currentQueryIndex == fetchConditions.Length - 1)
                {
                    if (cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                currentQueryIndex = (currentQueryIndex + 1) % fetchConditions.Length;
            } while (job == null);

            // TODO: There could be a conversion exception.
            var invocationData = JobHelper.FromJson<InvocationData>(job.InvocationData);

            return new JobPayload(
                job.Id.ToString(CultureInfo.InvariantCulture), 
                queueName, 
                invocationData)
            {
                Arguments = job.Arguments
            };
        }
    }
}