namespace Hangfire.SqlServer
{
    public class SqlServerStorageTableConfiguration
    {
        public SqlServerStorageTableConfiguration()
        {
            SchemaTableName = Constants.DefaultNameSchemaTable;
            JobTableName = Constants.DefaultNameJobTable;
            StateTableName = Constants.DefaultNameStateTable;
            JobParameterTableName = Constants.DefaultNameJobParameterTable;
            JobQueueTableName = Constants.DefaultNameJobQueueTable;
            ServerTableName = Constants.DefaultNameServerTable;
            HashTableName = Constants.DefaultNameHashTable;
            ListTableName = Constants.DefaultNameListTable;
            SetTableName = Constants.DefaultNameSetTable;
            ValueTableName = Constants.DefaultNameValueTable;
            CounterTableName = Constants.DefaultNameCounterTable;
            AggregatedCounterTableName = Constants.DefaultNameAggregatedCounterTable;
        }

        public string SchemaTableName { get; set; }
        public string JobTableName { get; set; }
        public string StateTableName { get; set;}
        public string JobParameterTableName { get; set; }
        public string JobQueueTableName { get; set; }
        public string ServerTableName { get; set; }
        public string HashTableName { get; set; }
        public string ListTableName { get; set; }
        public string SetTableName { get; set; }
        public string ValueTableName { get; set; }
        public string CounterTableName { get; set; }
        public string AggregatedCounterTableName { get; set; }

        public bool IsCompleteConfiguration =>
                !string.IsNullOrWhiteSpace(SchemaTableName) &&
                !string.IsNullOrWhiteSpace(JobTableName) &&
                !string.IsNullOrWhiteSpace(StateTableName) &&
                !string.IsNullOrWhiteSpace(JobParameterTableName) &&
                !string.IsNullOrWhiteSpace(JobQueueTableName) &&
                !string.IsNullOrWhiteSpace(ServerTableName) &&
                !string.IsNullOrWhiteSpace(HashTableName) &&
                !string.IsNullOrWhiteSpace(ListTableName) &&
                !string.IsNullOrWhiteSpace(SetTableName) &&
                !string.IsNullOrWhiteSpace(ValueTableName) &&
                !string.IsNullOrWhiteSpace(CounterTableName) &&
                !string.IsNullOrWhiteSpace(AggregatedCounterTableName);
    }
}
