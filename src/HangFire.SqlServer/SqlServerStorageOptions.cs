using System;

namespace HangFire.SqlServer
{
    public class SqlServerStorageOptions
    {
        private TimeSpan _queuePollInterval;

        public SqlServerStorageOptions()
        {
            QueuePollInterval = TimeSpan.FromSeconds(15);
            PrepareSchemaIfNecessary = true;
        }

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

        public bool PrepareSchemaIfNecessary { get; set; }
    }
}
