// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
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

        public SqlServerStorageOptions()
        {
            QueuePollInterval = TimeSpan.FromSeconds(15);
            SlidingInvisibilityTimeout = null;
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
        public bool UsePageLocksOnDequeue { get; set; }
        public bool UseRecommendedIsolationLevel { get; set; }
        public bool EnableHeavyMigrations { get; set; }
        public bool UseFineGrainedLocks { get; set; }

        /// <summary>
        /// Gets or sets whether IGNORE_DUP_OPTION was applied to [Hash] and [Set] tables and so MERGE
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
    }
}
