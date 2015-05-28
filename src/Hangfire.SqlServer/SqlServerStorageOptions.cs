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

#if DNXCORE50
using IsolationLevel = global::System.Data.IsolationLevel;
#else
using IsolationLevel = global::System.Transactions.IsolationLevel;
#endif

namespace Hangfire.SqlServer
{
    public class SqlServerStorageOptions
    {
        private TimeSpan _queuePollInterval;

        public SqlServerStorageOptions()
        {
            TransactionIsolationLevel = null;
            QueuePollInterval = TimeSpan.FromSeconds(15);
            InvisibilityTimeout = TimeSpan.FromMinutes(30);
            JobExpirationCheckInterval = TimeSpan.FromHours(1);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);
            PrepareSchemaIfNecessary = true;
        }

        public IsolationLevel? TransactionIsolationLevel { get; set; }

        public TimeSpan QueuePollInterval
        {
            get { return _queuePollInterval; }
            set
            {
                var message = String.Format(
                    "The QueuePollInterval property value should be positive. Given: {0}.",
                    value);

                if (value == TimeSpan.Zero)
                {
                    throw new ArgumentException(message, "value");
                }
                if (value != value.Duration())
                {
                    throw new ArgumentException(message, "value");
                }

                _queuePollInterval = value;
            }
        }

        public TimeSpan InvisibilityTimeout { get; set; }

        public bool PrepareSchemaIfNecessary { get; set; }

        public TimeSpan JobExpirationCheckInterval { get; set; }
        public TimeSpan CountersAggregateInterval { get; set; }
    }
}
