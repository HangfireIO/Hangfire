// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.Oracle
{
    public class OracleStorage : SqlStorage
    {
        public OracleStorage(string nameOrConnectionString) : base(nameOrConnectionString) {}

        public OracleStorage(string nameOrConnectionString, SqlStorageOptions options)
            : base(nameOrConnectionString, options) {}

        public override IMonitoringApi GetMonitoringApi()
        {
            return new OracleMonitoringApi(new OracleConnectionProvider(ConnectionString), SqlBook, QueueProviders);
        }

        protected override IConnectionProvider CreateConnectionProvider()
        {
            return new OracleConnectionProvider(ConnectionString);
        }

        protected override IPersistentJobQueueProvider GetDefaultPersistentJobQueueProvider()
        {
            return new OracleJobQueueProvider(ConnectionProvider, SqlBook, Options);
        }

        public override IStorageConnection GetConnection()
        {
            return new OracleStorageConnection(
                CreateAndOpenConnection(),
                SqlBook,
                new OracleDistributedLockAcquirer(),
                QueueProviders);
        }

        protected override SqlBook CreateSqlBook()
        {
            return new OracleSqlBook();
        }

        protected override ISchemaBuilder GetSchemaBuilder()
        {
            return new OracleShemaBuilder(ConnectionString);
        }

        public override string ToString()
        {
            return "OracleStorage: " + ConnectionString;
        }
    }
}