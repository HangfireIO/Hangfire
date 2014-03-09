using System;

namespace HangFire.SqlServer
{
    public class SqlServerStorageOptions
    {
        public SqlServerStorageOptions()
        {
            PollInterval = TimeSpan.FromSeconds(15);
            PrepareSchemaIfNecessary = true;
        }

        public TimeSpan PollInterval { get; set; }
        public bool PrepareSchemaIfNecessary { get; set; }
    }
}
