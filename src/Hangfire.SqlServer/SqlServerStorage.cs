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
using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    public class SqlServerStorage : SqlStorage {
        public SqlServerStorage(string nameOrConnectionString) : base(nameOrConnectionString) { }

        public SqlServerStorage(string nameOrConnectionString, SqlStorageOptions options)
            : base(nameOrConnectionString, options) {}

        protected override SqlBook CreateSqlBook() {
            return new SqlBook();
        }

        protected override ISchemaBuilder GetSchemaBuilder() {
            return new SqlServerObjectsInstaller();
        }

        protected override IConnectionProvider CreateConnectionProvider() {
            return new SqlStorageConnectionProvider(ConnectionString);
        }

        public override IMonitoringApi GetMonitoringApi() {
            return new SqlMonitoringApi(new SqlStorageConnectionProvider(ConnectionString), SqlBook, QueueProviders);
        }

        public override IStorageConnection GetConnection() {
            return new SqlStorageConnection(CreateAndOpenConnection(),
                                     SqlBook,
                                     new SqlServerDistributedLockAcquirer(),
                                     QueueProviders);
        }
    }
}