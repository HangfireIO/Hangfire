// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
#if FEATURE_CONFIGURATIONMANAGER
using System.Configuration;
#endif
#if FEATURE_TRANSACTIONSCOPE
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;
#endif
using Dapper;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    public class SqlServerStorage : JobStorage
    {
        private static readonly char[] SemicolonSeparator = new[] { ';' };
        private static readonly char[] EqualSignSeparator = new[] { '=' };

        private readonly ConcurrentDictionary<KeyValuePair<Func<string, string>, string>, string> _queryTemplateCache = new(new QueryTemplateKeyEqualityComparer());

        private readonly DbConnection _existingConnection;
        private readonly Func<DbConnection> _connectionFactory;
        private readonly SqlServerStorageOptions _options;
        private readonly string _connectionString;
        private string _escapedSchemaName;
        private SqlServerHeartbeatProcess _heartbeatProcess;

        private readonly Dictionary<string, bool> _features =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                { JobStorageFeatures.ExtendedApi, true },
                { JobStorageFeatures.JobQueueProperty, true },
                { JobStorageFeatures.ProcessesInsteadOfComponents, true },
                { JobStorageFeatures.Connection.BatchedGetFirstByLowest, true },
                { JobStorageFeatures.Connection.GetUtcDateTime, true },
                { JobStorageFeatures.Connection.GetSetContains, true },
                { JobStorageFeatures.Connection.LimitedGetSetCount, true },
                { JobStorageFeatures.Transaction.AcquireDistributedLock, true },
                { JobStorageFeatures.Transaction.CreateJob, false },
                { JobStorageFeatures.Transaction.SetJobParameter, false },
                { JobStorageFeatures.Monitoring.DeletedStateGraphs, true },
                { JobStorageFeatures.Monitoring.AwaitingJobs, true }
            };

        public SqlServerStorage(string nameOrConnectionString)
            : this(nameOrConnectionString, new SqlServerStorageOptions())
        {
        }

        /// <summary>
        /// Initializes SqlServerStorage from the provided SqlServerStorageOptions and either the provided connection
        /// string or the connection string with provided name pulled from the application config file.       
        /// </summary>
        /// <param name="nameOrConnectionString">Either a SQL Server connection string or the name of 
        /// a SQL Server connection string located in the connectionStrings node in the application config</param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrConnectionString"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="nameOrConnectionString"/> argument is neither 
        /// a valid SQL Server connection string nor the name of a connection string in the application
        /// config file.</exception>
        public SqlServerStorage(string nameOrConnectionString, SqlServerStorageOptions options)
        {
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _connectionString = GetConnectionString(nameOrConnectionString);
            _connectionFactory = DefaultConnectionFactory;
            _options = options;

            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// explicit instance of the <see cref="DbConnection"/> class that will be used
        /// to query the data.
        /// </summary>
        public SqlServerStorage([NotNull] DbConnection existingConnection)
            : this(existingConnection, new SqlServerStorageOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// explicit instance of the <see cref="DbConnection"/> class that will be used
        /// to query the data, with the given options.
        /// </summary>
        public SqlServerStorage([NotNull] DbConnection existingConnection, [NotNull] SqlServerStorageOptions options)
        {
            if (existingConnection == null) throw new ArgumentNullException(nameof(existingConnection));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _existingConnection = existingConnection;
            _options = options;

            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// a connection factory <see cref="Func{DbConnection}"/> class that will be invoked
        /// to create new database connections for querying the data.
        /// </summary>
        public SqlServerStorage([NotNull] Func<DbConnection> connectionFactory)
            : this(connectionFactory, new SqlServerStorageOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// a connection factory <see cref="Func{DbConnection}"/> class that will be invoked
        /// to create new database connections for querying the data.
        /// </summary>
        public SqlServerStorage([NotNull] Func<DbConnection> connectionFactory, [NotNull] SqlServerStorageOptions options)
        {
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _connectionFactory = connectionFactory;
            _options = options;

            Initialize();
        }

        public virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public override bool LinearizableReads => true;

        internal string SchemaName => _escapedSchemaName;
        internal int? CommandTimeout => _options.CommandTimeout.HasValue ? (int)_options.CommandTimeout.Value.TotalSeconds : (int?)null;
        internal int? CommandBatchMaxTimeout => _options.CommandBatchMaxTimeout.HasValue ? (int)_options.CommandBatchMaxTimeout.Value.TotalSeconds : (int?)null;
        internal TimeSpan? SlidingInvisibilityTimeout => _options.SlidingInvisibilityTimeout;
        internal SqlServerStorageOptions Options => _options;
        internal SqlServerHeartbeatProcess HeartbeatProcess => _heartbeatProcess;

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(this, _options.DashboardJobListLimit);
        }

        public override IStorageConnection GetConnection()
        {
            return new SqlServerConnection(this);
        }

#pragma warning disable 618
        [Obsolete($"Please use the `{nameof(GetStorageWideProcesses)}` and/or `{nameof(GetServerRequiredProcesses)}` methods instead, and enable `{nameof(JobStorageFeatures)}.{nameof(JobStorageFeatures.ProcessesInsteadOfComponents)}`. Will be removed in 2.0.0.")]
        public override IEnumerable<IServerComponent> GetComponents()
#pragma warning restore 618
        {
            yield return new ExpirationManager(this, _options.InactiveStateExpirationTimeout, _options.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _options.CountersAggregateInterval);
            yield return _heartbeatProcess;
        }

        public override IEnumerable<IBackgroundProcess> GetServerRequiredProcesses()
        {
            yield return _heartbeatProcess;
        }

        public override IEnumerable<IBackgroundProcess> GetStorageWideProcesses()
        {
            yield return new ExpirationManager(this, _options.InactiveStateExpirationTimeout, _options.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _options.CountersAggregateInterval);
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info($"Using the following options for SQL Server job storage: Queue poll interval: {_options.QueuePollInterval}.");
        }

        public override bool HasFeature(string featureId)
        {
            if (featureId == null) throw new ArgumentNullException(nameof(featureId));

            return _features.TryGetValue(featureId, out var isSupported) 
                ? isSupported
                : base.HasFeature(featureId);
        }

        public override string ToString()
        {
            const string canNotParseMessage = "<Connection string can not be parsed>";

            try
            {
                if (_connectionString == null)
                {
                    return "SQL Server (custom)";
                }

                var parts = _connectionString.Split(SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(static x => x.Split(EqualSignSeparator, StringSplitOptions.RemoveEmptyEntries))
                    .Select(static x => new { Key = x[0].Trim(), Value = x[1].Trim() })
                    .GroupBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(static x => x.Key, static x => x.Last().Value, StringComparer.OrdinalIgnoreCase);

                var builder = new StringBuilder();

                foreach (var alias in new[] { "Data Source", "Server", "Address", "Addr", "Network Address" })
                {
                    if (parts.TryGetValue(alias, out var part))
                    {
                        builder.Append(part);
                        break;
                    }
                }

                if (builder.Length != 0) builder.Append('@');

                foreach (var alias in new[] { "Database", "Initial Catalog" })
                {
                    if (parts.TryGetValue(alias, out var part))
                    {
                        builder.Append(part);
                        break;
                    }
                }

                return builder.Length != 0
                    ? $"SQL Server: {builder}"
                    : canNotParseMessage;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                return canNotParseMessage;
            }
        }

        internal string GetQueryFromTemplate(Func<string, string> templateFunc)
        {
            return _queryTemplateCache.GetOrAdd(
                new KeyValuePair<Func<string, string>, string>(templateFunc, SchemaName),
                static pair => pair.Key(pair.Value));
        }

        internal void UseConnection(
            DbConnection dedicatedConnection,
            [InstantHandle] Action<SqlServerStorage, DbConnection> action)
        {
            UseConnection(dedicatedConnection, static (storage, connection, ctx) =>
            {
                ctx(storage, connection);
                return true;
            }, action);
        }

        internal TResult UseConnection<TResult>(
            DbConnection dedicatedConnection,
            [InstantHandle] Func<SqlServerStorage, DbConnection, TResult> action)
        {
            return UseConnection(dedicatedConnection, static (storage, connection, ctx) => ctx(storage, connection), action);
        }

        internal TResult UseConnection<TContext, TResult>(
            DbConnection dedicatedConnection,
            [InstantHandle] Func<SqlServerStorage, DbConnection, TContext, TResult> func,
            TContext context)
        {
            DbConnection connection = null;

            try
            {
                connection = dedicatedConnection ?? CreateAndOpenConnection();
                return func(this, connection, context);
            }
            finally
            {
                if (dedicatedConnection == null)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        internal void UseTransaction<TContext>(
            DbConnection dedicatedConnection,
            [InstantHandle] Action<SqlServerStorage, DbConnection, DbTransaction, TContext> action,
            TContext context)
        {
            UseTransaction(dedicatedConnection, static (storage, connection, transaction, ctx) =>
            {
                ctx.Key(storage, connection, transaction, ctx.Value);
                return true;
            }, new KeyValuePair<Action<SqlServerStorage, DbConnection, DbTransaction, TContext>, TContext>(action, context), null);
        }
        
        internal TResult UseTransaction<TContext, TResult>(
            DbConnection dedicatedConnection,
            [InstantHandle] Func<SqlServerStorage, DbConnection, DbTransaction, TContext, TResult> func,
            TContext context,
            IsolationLevel? isolationLevel)
        {
            isolationLevel = isolationLevel ?? (_options.UseRecommendedIsolationLevel
                ? IsolationLevel.ReadCommitted
#pragma warning disable 618
                : _options.TransactionIsolationLevel);
#pragma warning restore 618

#if FEATURE_TRANSACTIONSCOPE
            if (IsRunningOnWindows() && !_options.DisableTransactionScope)
            {
                using (var transaction = CreateTransaction(isolationLevel))
                {
                    var result = UseConnection(dedicatedConnection, static (storage, connection, ctx) =>
                    {
                        connection.EnlistTransaction(Transaction.Current);
                        return ctx.Key(storage, connection, null, ctx.Value);
                    }, new KeyValuePair<Func<SqlServerStorage, DbConnection, DbTransaction, TContext, TResult>, TContext>(func, context));

                    transaction.Complete();

                    return result;
                }
            }
            else
#endif
            {
                return UseConnection(dedicatedConnection, static (storage, connection, ctx) =>
                {
                    using (var transaction = connection.BeginTransaction(
#if !FEATURE_TRANSACTIONSCOPE
                        ctx.Value.Value ??
#endif
                        System.Data.IsolationLevel.ReadCommitted))
                    {
                        TResult result;

                        try
                        {
                            result = ctx.Key(storage, connection, transaction, ctx.Value.Key);
                            transaction.Commit();
                        }
                        catch (Exception ex) when (ex.IsCatchableExceptionType())
                        {
                            // It is possible that XACT_ABORT option is set, and in this
                            // case transaction will be aborted automatically on server.
                            // Some older SqlClient implementations throw InvalidOperationException
                            // when trying to rollback such an aborted transaction, so we
                            // try to handle this case.
                            //
                            // It's also possible that our connection is broken, so this
                            // check is useful even when XACT_ABORT option wasn't set.
                            if (transaction.Connection != null)
                            {
                                // Don't rely on implicit rollback when calling the Dispose
                                // method, because some implementations may throw the
                                // NullReferenceException, although it's prohibited to throw
                                // any exception from a Dispose method, according to the
                                // .NET Framework Design Guidelines:
                                // https://github.com/dotnet/efcore/issues/12864
                                // https://github.com/HangfireIO/Hangfire/issues/1494
                                transaction.Rollback();
                            }

                            throw;
                        }

                        return result;
                    }
                }, new KeyValuePair<Func<SqlServerStorage, DbConnection, DbTransaction, TContext, TResult>, KeyValuePair<TContext, IsolationLevel?>>(
                    func,
                    new KeyValuePair<TContext, IsolationLevel?>(context, isolationLevel)));
            }
        }

        internal DbConnection CreateAndOpenConnection()
        {
            using (_options.ImpersonationFunc?.Invoke())
            {
                DbConnection connection = null;

                try
                {
                    connection = _existingConnection ?? _connectionFactory();

                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    return connection;
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    ReleaseConnection(connection);
                    throw;
                }
            }
        }

        internal bool IsExistingConnection(IDbConnection connection)
        {
            return connection != null && ReferenceEquals(connection, _existingConnection);
        }

        internal void ReleaseConnection(IDbConnection connection)
        {
            if (connection != null && !IsExistingConnection(connection))
            {
                connection.Dispose();
            }
        }
        
        private DbConnection DefaultConnectionFactory()
        {
            var connection = _options.SqlClientFactory.CreateConnection() ?? throw new InvalidOperationException($"The provider factory ({_options.SqlClientFactory}) returned a null DbConnection.");
            connection.ConnectionString = _connectionString;
            return connection;
        }

        private static bool IsRunningOnWindows()
        {
#if !NETSTANDARD1_3
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#endif
        }

        private void Initialize()
        {
            _escapedSchemaName = _options.SchemaName.Replace("]", "]]");

            if (_options.PrepareSchemaIfNecessary)
            {
                var log = LogProvider.GetLogger(typeof(SqlServerObjectsInstaller));
                const int RetryAttempts = 3;

                log.Info("Start installing Hangfire SQL objects...");

                Exception lastException = null;

                for (var i = 0; i < RetryAttempts; i++)
                {
                    try
                    {
                        UseConnection(null, static (storage, connection) =>
                        {
                            // TODO: Escape schema here???
                            SqlServerObjectsInstaller.Install(connection, storage.Options.SchemaName, storage.Options.EnableHeavyMigrations);
                        });

                        lastException = null;
                        break;
                    }
                    catch (DbException ex)
                    {
                        lastException = ex;
                        log.WarnException("An exception occurred while trying to perform the migration." + (i < RetryAttempts - 1 ? " Retrying..." : ""), ex);
                    }
                }

                if (lastException != null)
                {
                    log.WarnException("Was unable to perform the Hangfire schema migration due to an exception. Ignore this message unless you've just installed or upgraded Hangfire.", lastException);
                }
                else
                {
                    log.Info("Hangfire SQL objects installed.");
                }
            }

            if (_options.TryAutoDetectSchemaDependentOptions)
            {
                try
                {
                    int? schema = UseConnection(null, static (storage, connection) =>
                        connection.ExecuteScalar<int>($"select top (1) [Version] from [{storage.SchemaName}].[Schema]"));

                    _options.UseRecommendedIsolationLevel = true;
                    _options.UseIgnoreDupKeyOption = schema >= 8;
                    _options.DisableGlobalLocks = schema >= 6;

                    if (schema >= 6 && _options.DeleteExpiredBatchSize == -1)
                    {
                        _options.DeleteExpiredBatchSize = 10000;
                    }
                }
                catch (Exception ex)
                {
                    var log = LogProvider.GetLogger(typeof(SqlServerStorage));
                    log.ErrorException("Was unable to use the TryAutoDetectSchemaDependentOptions option due to an exception.", ex);
                }
            }

            InitializeQueueProviders();
            _heartbeatProcess = new SqlServerHeartbeatProcess();

            _features.Add(
                JobStorageFeatures.Transaction.RemoveFromQueue(typeof(SqlServerTimeoutJob)),
                _options.UseTransactionalAcknowledge);
        }

        private void InitializeQueueProviders()
        {
            var defaultQueueProvider = _options.DefaultQueueProvider ?? new SqlServerJobQueueProvider(this, _options);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);
        }

        private static string GetConnectionString(string nameOrConnectionString)
        {
#if FEATURE_CONFIGURATIONMANAGER
            if (IsConnectionString(nameOrConnectionString))
            {
                return nameOrConnectionString;
            }

            if (IsConnectionStringInConfiguration(nameOrConnectionString))
            {
                return ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }

            throw new ArgumentException(
                $"Could not find connection string with name '{nameOrConnectionString}' in application config file");
#else
            return nameOrConnectionString;
#endif
        }

#if FEATURE_CONFIGURATIONMANAGER
        private static bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }

        private static bool IsConnectionStringInConfiguration(string connectionStringName)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }
#endif

#if FEATURE_TRANSACTIONSCOPE
        private TransactionScope CreateTransaction(IsolationLevel? isolationLevel)
        {
            return isolationLevel != null
                ? new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = isolationLevel.Value, Timeout = _options.TransactionTimeout })
                : new TransactionScope();
        }
#endif

        public static readonly DashboardMetric ActiveConnections = new DashboardMetric(
            "connections:active",
            "Metrics_ActiveConnections",
            static page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (_, connection) =>
                {
                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background' and status != 'sleeping'";

                    var value = connection
                        .QuerySingle<int>(sqlQuery, new { name = connection.Database });

                    return new Metric(value);
                });
            });

        public static readonly DashboardMetric TotalConnections = new DashboardMetric(
            "connections:total",
            "Metrics_TotalConnections",
            static page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (_, connection) =>
                {
                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background'";

                    var value = connection
                        .QuerySingle<int>(sqlQuery, new { name = connection.Database });

                    return new Metric(value);
                });
            });

        public static readonly DashboardMetric ActiveTransactions = new DashboardMetric(
            "transactions:active",
            "Metrics_SQLServer_ActiveTransactions",
            static page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (_, connection) =>
                {
                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background' and open_tran = 1";

                    var value = connection
                        .QuerySingle<int>(sqlQuery, new { name = connection.Database });

                    return new Metric(value);
                });
            });

        public static readonly DashboardMetric DataFilesSize = new DashboardMetric(
            "database:files:rows:size",
            "Metrics_SQLServer_DataFilesSize",
            static page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (_, connection) =>
                {
                    var sqlQuery = @"
select SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS INT)/128.0) as RowsSizeMB from sys.database_files
where type = 0;";

                    var value = connection.QuerySingle<double>(sqlQuery);

                    return new Metric(value.ToString("F", CultureInfo.CurrentCulture));
                });
            });

        public static readonly DashboardMetric LogFilesSize = new DashboardMetric(
            "database:files:log:size",
            "Metrics_SQLServer_LogFilesSize",
            static page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (_, connection) =>
                {
                    var sqlQuery = @"
select SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS INT)/128.0) as LogSizeMB from sys.database_files
where type = 1;";

                    var value = connection.QuerySingle<double>(sqlQuery);

                    return new Metric(value.ToString("F", CultureInfo.CurrentCulture));
                });
            });

        public static readonly DashboardMetric SchemaVersion = new DashboardMetric(
            "sqlserver:schema",
            "Metrics_SQLServer_SchemaVersion",
            static page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (storage, connection) =>
                {
                    var sqlQuery = $@"select top(1) [Version] from [{storage.SchemaName}].[Schema]";
                    var version = connection.QuerySingleOrDefault<int?>(sqlQuery);

                    if (!version.HasValue)
                    {
                        return new Metric("Unspecified")
                        {
                            Style = MetricStyle.Danger,
                        };
                    }

                    return new Metric(version.Value)
                    {
                        Style = version < SqlServerObjectsInstaller.LatestSchemaVersion
                            ? MetricStyle.Warning
                            : version == SqlServerObjectsInstaller.LatestSchemaVersion
                                ? MetricStyle.Success
                                : MetricStyle.Default
                    };
                });
            });

        public static readonly Func<string, string, string, DashboardMetric> PerformanceCounterDatabaseMetric = 
            static (string objectName, string counterName, string instanceName) => new DashboardMetric(
            $"sqlserver:counter:{objectName}:{counterName}:{instanceName ?? "db"}",
            counterName,
            page =>
            {
                if (objectName == null) throw new ArgumentNullException(nameof(objectName));
                if (counterName == null) throw new ArgumentNullException(nameof(counterName));

                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, static (_, connection, ctx) =>
                {
                    var sqlQuery = $@"SELECT cntr_value FROM sys.dm_os_performance_counters where object_name = @objectName and instance_name = @instanceName and counter_name = @counterName";
                    long? value;

                    try
                    {
                        value = connection.QuerySingle<long>(sqlQuery, new
                        {
                            objectName = ctx.Item1,
                            instanceName = ctx.Item2 ?? connection.Database,
                            counterName = ctx.Item3
                        });
                    }
                    catch
                    {
                        value = null;
                    }

                    return value != null ? new Metric(value.Value) : new Metric("???");
                }, Tuple.Create(objectName, instanceName, counterName));
            });

        private sealed class
            QueryTemplateKeyEqualityComparer : IEqualityComparer<KeyValuePair<Func<string, string>, string>>
        {
            public bool Equals(KeyValuePair<Func<string, string>, string> x, KeyValuePair<Func<string, string>, string> y)
            {
                return x.Key.Equals(y.Key) && x.Value == y.Value;
            }

            public int GetHashCode(KeyValuePair<Func<string, string>, string> obj)
            {
                unchecked
                {
                    return (obj.Key.GetHashCode() * 397) ^ obj.Value.GetHashCode();
                }
            }
        }
    }
}