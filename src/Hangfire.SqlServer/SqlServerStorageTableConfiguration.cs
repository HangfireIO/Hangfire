using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.SqlServer
{
    public class SqlServerStorageTableConfiguration
    {
        private readonly IDictionary<string, string> _customTableNames;

        public SqlServerStorageTableConfiguration()
        {
            _customTableNames = new Dictionary<string, string>();

            _customTableNames["Schema"] = Constants.DefaultNameSchemaTable;
            _customTableNames["Job"] = Constants.DefaultNameJobTable;
            _customTableNames["State"] = Constants.DefaultNameStateTable;
            _customTableNames["JobParameter"] = Constants.DefaultNameJobParameterTable;
            _customTableNames["JobQueue"] = Constants.DefaultNameJobQueueTable;
            _customTableNames["Server"] = Constants.DefaultNameServerTable;
            _customTableNames["Hash"] = Constants.DefaultNameHashTable;
            _customTableNames["List"] = Constants.DefaultNameListTable;
            _customTableNames["Set"] = Constants.DefaultNameSetTable;
            _customTableNames["Value"] = Constants.DefaultNameValueTable;
            _customTableNames["Counter"] = Constants.DefaultNameCounterTable;
            _customTableNames["AggregatedCounter"] = Constants.DefaultNameAggregatedCounterTable;
        }

        public bool Equals(SqlServerStorageTableConfiguration otherConfiguration)
        {
            if (otherConfiguration == null) return false;
            return _customTableNames.All(kvp => kvp.Value == otherConfiguration[kvp.Key]);
        }

        public ICollection<string> AvailableConfigurableTableNames => _customTableNames.Keys.ToList();

        public string this[string tableName]
        {
            get
            {
                return _customTableNames[tableName];
            }
            set
            {
                if (value == null || string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _customTableNames[tableName] = value;
            }
        }
    }
}
