using System;

namespace HangFire.SqlServer
{
    public class SqlServerStorageOptions
    {
        public SqlServerStorageOptions()
        {
            PollInterval = TimeSpan.FromSeconds(15);
        }

        public TimeSpan PollInterval { get; set; }
    }
}
