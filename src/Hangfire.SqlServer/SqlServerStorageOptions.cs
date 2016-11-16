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
#if NETFULL
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

        public SqlServerStorageOptions()
        {
            TransactionIsolationLevel = null;
            QueuePollInterval = TimeSpan.FromSeconds(15);
#pragma warning disable 618
            InvisibilityTimeout = TimeSpan.FromMinutes(30);
#pragma warning restore 618
            JobExpirationCheckInterval = TimeSpan.FromMinutes(30);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);
            PrepareSchemaIfNecessary = true;
            DashboardJobListLimit = 10000;
            _schemaName = Constants.DefaultSchema;
            TransactionTimeout = TimeSpan.FromMinutes(1);
        }

        public IsolationLevel? TransactionIsolationLevel { get; set; }

        public TimeSpan QueuePollInterval
        {
            get { return _queuePollInterval; }
            set
            {
                var message = $"The QueuePollInterval property value should be positive. Given: {value}.";

                if (value == TimeSpan.Zero)
                {
                    throw new ArgumentException(message, nameof(value));
                }
                if (value != value.Duration())
                {
                    throw new ArgumentException(message, nameof(value));
                }

                _queuePollInterval = value;
            }
        }

        [Obsolete("Does not make sense anymore. Background jobs re-queued instantly even after ungraceful shutdown now. Will be removed in 2.0.0.")]
        public TimeSpan InvisibilityTimeout { get; set; }

        public bool PrepareSchemaIfNecessary { get; set; }

        public TimeSpan JobExpirationCheckInterval { get; set; }
        public TimeSpan CountersAggregateInterval { get; set; }

        public int? DashboardJobListLimit { get; set; }
        public TimeSpan TransactionTimeout { get; set; }

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
    }
}
