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

namespace Hangfire.Sql
{
    public class SqlJobQueueProvider : IPersistentJobQueueProvider
    {
        protected IConnectionProvider ConnectionProvider { get; private set; }
        protected SqlBook SqkBook { get; private set; }
        public SqlStorageOptions Options { get; private set; }

        public SqlJobQueueProvider(IConnectionProvider connectionProvider, SqlBook sqkBook, SqlStorageOptions options)
        {
            if (connectionProvider == null) throw new ArgumentNullException("connectionProvider");
            if (options == null) throw new ArgumentNullException("options");

            ConnectionProvider = connectionProvider;
            SqkBook = sqkBook;
            Options = options;
        }

        public virtual IPersistentJobQueue GetJobQueue()
        {
            return new SqlJobQueue(ConnectionProvider, SqkBook, Options);
        }

        public virtual IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi()
        {
            return new SqlJobQueueMonitoringApi(ConnectionProvider, SqkBook);
        }
    }
}