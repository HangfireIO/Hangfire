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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.Sql
{
    public abstract class SqlStorage : JobStorage {
        private static object _sync = new object();
        private SqlBook _sqlBook;
        private IConnectionProvider _connectionProvider;
        protected SqlStorageOptions Options { get; private set; }
        protected string ConnectionString { get; private set; }

        public PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public SqlBook SqlBook {
            get {
                lock (_sync) {
                    if (_sqlBook == null) {
                        _sqlBook = CreateSqlBook();
                    }
                }
                return _sqlBook;
                
            }
        }

        public IConnectionProvider ConnectionProvider {
            get {
                lock (_sync) {
                    if (_connectionProvider == null) {
                        _connectionProvider = CreateConnectionProvider();
                    }
                }
                return _connectionProvider;
            }
        }


        protected SqlStorage(string nameOrConnectionString)
            : this(nameOrConnectionString, new SqlStorageOptions()) {
        }

        protected abstract SqlBook CreateSqlBook();

        /// <summary>
        /// Initializes SqlStorage from the provided SqlStorageOptions and either the provided connection
        /// string or the connection string with provided name pulled from the application config file.       
        /// </summary>
        /// <param name="nameOrConnectionString">Either a SQL Server connection string or the name of 
        ///     a SQL Server connection string located in the connectionStrings node in the application config</param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrConnectionString"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="nameOrConnectionString"/> argument is neither 
        /// a valid SQL Server connection string nor the name of a connection string in the application
        /// config file.</exception>
        protected SqlStorage(string nameOrConnectionString, SqlStorageOptions options)
        {
            if (nameOrConnectionString == null) throw new ArgumentNullException("nameOrConnectionString");
            if (options == null) throw new ArgumentNullException("options");

            Options = options;
            ConnectionString = ConnectionStringResolver.GetConnectionString(nameOrConnectionString);
            QueueProviders = new PersistentJobQueueProviderCollection(GetDefaultPersistentJobQueueProvider());

            if (!options.PrepareSchemaIfNecessary) 
            {
                return;
            }
            using (var connection = CreateAndOpenConnection())
            {
                GetSchemaBuilder().BuildDatabase(connection);
            }
        }

        public IDbConnection CreateAndOpenConnection() {
            return ConnectionProvider.CreateAndOpenConnection();
        }

        protected abstract ISchemaBuilder GetSchemaBuilder();

        protected virtual IPersistentJobQueueProvider GetDefaultPersistentJobQueueProvider() {
            return new SqlJobQueueProvider(ConnectionProvider, SqlBook, Options); 
        }

        protected abstract IConnectionProvider CreateConnectionProvider();

        public override IEnumerable<IServerComponent> GetComponents()
        {
            yield return new ExpirationManager(this);
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for SQL job storage:");
            logger.InfoFormat("    Queue poll interval: {0}.", Options.QueuePollInterval);
            logger.InfoFormat("    Invisibility timeout: {0}.", Options.InvisibilityTimeout);
        }

        public override string ToString()
        {
            const string canNotParseMessage = "<Connection string can not be parsed>";

            try {
                var parts = ConnectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => new { Key = x[0].Trim(), Value = x[1].Trim() })
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                var builder = new StringBuilder();

                foreach (var alias in new[] { "Data Source", "Server", "Address", "Addr", "Network Address" }) {
                    if (parts.ContainsKey(alias)) {
                        builder.Append(parts[alias]);
                        break;
                    }
                }

                if (builder.Length != 0) builder.Append("@");

                foreach (var alias in new[] { "Database", "Initial Catalog" }) {
                    if (parts.ContainsKey(alias)) {
                        builder.Append(parts[alias]);
                        break;
                    }
                }

                return builder.Length != 0
                    ? String.Format("SQL Server: {0}", builder)
                    : canNotParseMessage;
            }
            catch (Exception) {
                return canNotParseMessage;
            }
        }
    }
}