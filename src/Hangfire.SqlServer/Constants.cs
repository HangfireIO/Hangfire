namespace Hangfire.SqlServer
{
    internal class Constants
    {
        public static readonly string DefaultSchema = "HangFire";
        
        //Default table names
        public const string DefaultNameSchemaTable = "Schema";
        public const string DefaultNameJobTable = "Job";
        public const string DefaultNameStateTable = "State";
        public const string DefaultNameJobParameterTable = "JobParameter";
        public const string DefaultNameJobQueueTable = "JobQueue";
        public const string DefaultNameServerTable = "Server";
        public const string DefaultNameHashTable = "Hash";
        public const string DefaultNameListTable = "List";
        public const string DefaultNameSetTable = "Set";
        public const string DefaultNameValueTable = "Value";
        public const string DefaultNameCounterTable = "Counter";
        public const string DefaultNameAggregatedCounterTable = "AggregatedCounter";
    }
}
