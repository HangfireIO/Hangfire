using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using HangFire.Server;
using HangFire.Storage;
using HangFire.Storage.Monitoring;

namespace HangFire.SqlServer
{
    public class SqlServerStorage : JobStorage
    {
        private readonly string _connectionString;

        public SqlServerStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override IMonitoringApi Monitoring
        {
            get { return new SqlServerMonitoringApi(new SqlConnection(_connectionString)); }
        }

        public override IStorageConnection CreateConnection()
        {
            return CreatePooledConnection();
        }

        public override IStorageConnection CreatePooledConnection()
        {
            return new SqlStorageConnection(new SqlConnection(_connectionString));
        }

        public override IJobFetcher CreateFetcher(IEnumerable<string> queues, int workersCount)
        {
            return new SqlServerFetcher(new SqlConnection(_connectionString), queues);
        }

        public override IEnumerable<IThreadWrappable> GetComponents()
        {
            return Enumerable.Empty<IThreadWrappable>();
        }
    }
}