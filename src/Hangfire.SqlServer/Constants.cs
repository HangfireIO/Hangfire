namespace Hangfire.SqlServer
{
    internal class Constants
    {
        public static readonly string DefaultSchema = "HangFire";
        
        //Default table names
        public static readonly string DefaultNameSchemaTable = "Schema";
        public static readonly string DefaultNameJobTable = "Job";
        public static readonly string DefaultNameStateTable = "State";
        public static readonly string DefaultNameJobParameterTable = "JobParameter";
        public static readonly string DefaultNameJobQueueTable = "JobQueue";
        public static readonly string DefaultNameServerTable = "Server";
        public static readonly string DefaultNameHashTable = "Hash";
        public static readonly string DefaultNameListTable = "List";
        public static readonly string DefaultNameSetTable = "Set";
        public static readonly string DefaultNameValueTable = "Value";
        public static readonly string DefaultNameCounterTable = "Counter";
        public static readonly string DefaultNameAggregatedCounterTable = "AggregatedCounter";
    }
}
