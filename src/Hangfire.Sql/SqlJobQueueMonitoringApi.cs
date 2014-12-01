﻿// This file is part of Hangfire.
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
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace Hangfire.Sql
{
    internal class SqlJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly IConnectionProvider _connectionProvider;
        private readonly SqlBook _sqlBook;

        public SqlJobQueueMonitoringApi(IConnectionProvider connectionProvider, SqlBook sqlBook)
        {
            if (connectionProvider == null) throw new ArgumentNullException("connectionProvider");
            _connectionProvider = connectionProvider;
            _connectionProvider = connectionProvider;
            _sqlBook = sqlBook;
        }

        public IEnumerable<string> GetQueues()
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection()) {
                return connection.Query(_sqlBook.SqlJobQueueMonitoringApi_GetQueues).Select(x => (string)x.Queue).ToList();                
            }
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection()) {
                return connection.Query<JobIdDto>(
                    _sqlBook.SqlJobQueueMonitoringApi_GetEnqueuedJobIds,
                    new {queue = queue, pstart = from + 1, pend = @from + perPage})
                    .ToList()
                    .Select(x => x.Id)
                    .ToList();
            }
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection()) {
                return connection.Query<JobIdDto>(
                    _sqlBook.SqlJobQueueMonitoringApi_GetFetchedJobIds,
                    new {queue = queue, start = from + 1, end = @from + perPage})
                    .ToList()
                    .Select(x => x.Id)
                    .ToList();
            }
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            using (var connection = _connectionProvider.CreateAndOpenConnection()) {
                var result =
                    connection.Query(_sqlBook.SqlJobQueueMonitoringApi_GetEnqueuedAndFetchedCount, new { queue = queue })
                        .Single();

                return new EnqueuedAndFetchedCountDto {
                    EnqueuedCount = result.EnqueuedCount,
                    FetchedCount = result.FetchedCount
                };
            }
        }

// ReSharper disable once ClassNeverInstantiated.Local
        private class JobIdDto
        {
            public int Id { get; set; }
        }
    }
}