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
using System.Data.Common;
using System.Reflection;
#if FEATURE_TRANSACTIONSCOPE
using System.Transactions;
#else
using System.Data;
#endif

namespace Hangfire.SqlServer
{
    public class SqlServerStorageOptions
    {
        private TimeSpan _queuePollInterval;
        private string _schemaName;
        private TimeSpan _jobExpirationCheckInterval;
        private TimeSpan? _slidingInvisibilityTimeout;
        private DbProviderFactory _sqlClientFactory;

        public SqlServerStorageOptions()
        {
            QueuePollInterval = TimeSpan.Zero;
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5);
#pragma warning disable 618
            InvisibilityTimeout = TimeSpan.FromMinutes(30);
#pragma warning restore 618
            JobExpirationCheckInterval = TimeSpan.FromMinutes(30);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);
            PrepareSchemaIfNecessary = true;
            DashboardJobListLimit = 10000;
            _schemaName = Constants.DefaultSchema;
            TransactionTimeout = TimeSpan.FromMinutes(1);
            DisableGlobalLocks = false;
            UsePageLocksOnDequeue = false;
            DeleteExpiredBatchSize = -1;
            UseTransactionalAcknowledge = false;
            UseRecommendedIsolationLevel = true;
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5);
            TryAutoDetectSchemaDependentOptions = true;
            _sqlClientFactory = GetDefaultSqlClientFactory();
        }

        private static DbProviderFactory GetDefaultSqlClientFactory()
        {
            var dbProviderFactoryTypes = new[]
            {
                // Available in the .NET Framework GAC, requires Version + Culture + PublicKeyToken to be explicitly specified
                "System.Data.SqlClient.SqlClientFactory, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient",
                "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient",
            };

            foreach (var dbProviderFactoryType in dbProviderFactoryTypes)
            {
                var providerFactoryType = Type.GetType(dbProviderFactoryType, throwOnError: false);
                if (providerFactoryType != null)
                {
                    var instanceField = providerFactoryType.GetField("Instance");
                    if (instanceField == null)
                    {
                        continue;
                    }
                    var instance = (DbProviderFactory)instanceField.GetValue(null);
                    if (instance != null)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }

        [Obsolete("TransactionIsolationLevel option is deprecated, please set UseRecommendedIsolationLevel instead. Will be removed in 2.0.0.")]
        public IsolationLevel? TransactionIsolationLevel { get; set; }

        public TimeSpan QueuePollInterval
        {
            get { return _queuePollInterval; }
            set
            {
                var message = $"The QueuePollInterval property value should be positive. Given: {value}.";

                if (value != value.Duration())
                {
                    throw new ArgumentException(message, nameof(value));
                }

                _queuePollInterval = value;
            }
        }

        [Obsolete("Does not make sense anymore. Background jobs re-queued instantly even after ungraceful shutdown now. Will be removed in 2.0.0.")]
        public TimeSpan InvisibilityTimeout { get; set; }

        public TimeSpan? SlidingInvisibilityTimeout
        {
            get { return _slidingInvisibilityTimeout; }
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("Sliding timeout should be greater than zero");
                }

                _slidingInvisibilityTimeout = value;
            }
        }

        public bool PrepareSchemaIfNecessary { get; set; }

        public TimeSpan JobExpirationCheckInterval 
        {
            get { return _jobExpirationCheckInterval; } 
            set {
                if (value.TotalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("Job expiration check interval cannot be greater than int.MaxValue");
                }
                _jobExpirationCheckInterval = value;
            }
        }

        public TimeSpan CountersAggregateInterval { get; set; }

        public int? DashboardJobListLimit { get; set; }
        public TimeSpan TransactionTimeout { get; set; }
        public TimeSpan? CommandTimeout { get; set; }
        public TimeSpan? CommandBatchMaxTimeout { get; set; }

        public string SchemaName
        {
            get { return _schemaName; }
            set
            {
                if (string.IsNullOrWhiteSpace(_schemaName))
                {
                    throw new ArgumentException(_schemaName, nameof(value));
                }
                _schemaName = value;
            }
        }

        public Func<IDisposable> ImpersonationFunc { get; set; }
        public bool DisableGlobalLocks { get; set; }
        
        [Obsolete("This option is deprecated and doesn't change anything. You can safely remove it. Will be removed in 2.0.0.")]
        public bool UsePageLocksOnDequeue { get; set; }
        public bool UseRecommendedIsolationLevel { get; set; }
        public bool EnableHeavyMigrations { get; set; }
        public bool UseFineGrainedLocks { get; set; }

        /// <summary>
        /// Gets or sets whether IGNORE_DUP_KEY was applied to [Hash] and [Set] tables and so MERGE
        /// statements can be replaced by much more efficient INSERT/UPDATE pair. This option allows
        /// to avoid deadlocks related to SERIALIZABLE-level range locks without introducing transient
        /// errors due to concurrency.
        /// </summary>
        public bool UseIgnoreDupKeyOption { get; set; }

        /// <summary>
        /// Gets or sets the number of records deleted in a single batch in expiration manager. Default
        /// value is 1000, but it can be configured to a higher one when processing throughput is high
        /// enough, so expiration manager becomes the bottleneck.
        /// </summary>
        public int DeleteExpiredBatchSize { get; set; }

        /// <summary>
        /// Gets or sets whether to enable experimental feature of transactional acknowledge of completed
        /// background jobs. In this case there will be less requests sent to SQL Server and better handling
        /// of data loss when asynchronous replication is used. But additional blocking on the JobQueue table
        /// is expected, since transaction commit requires an explicit Commit request to be sent. 
        /// </summary>
        public bool UseTransactionalAcknowledge { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbProviderFactory"/> for creating <c>SqlConnection</c> instances.
        /// Defaults to either <c>System.Data.SqlClient.SqlClientFactory.Instance</c> or
        /// <c>Microsoft.Data.SqlClient.SqlClientFactory</c> depending on which package reference exists
        /// on the consuming project.
        /// </summary>
        public DbProviderFactory SqlClientFactory
        {
            get => _sqlClientFactory ?? throw new InvalidOperationException("A reference to either Microsoft.Data.SqlClient or System.Data.SqlClient must exist.");
            set => _sqlClientFactory = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets whether to try automatically query for the current schema on application start
        /// and enable <see cref="UseIgnoreDupKeyOption"/>, <see cref="DeleteExpiredBatchSize"/> and
        /// <see cref="DisableGlobalLocks"/> options depending on the current schema version. When storage
        /// is inaccessible on startup, default values will be used for those options.
        /// </summary>
        public bool TryAutoDetectSchemaDependentOptions { get; set; }
    }
}
