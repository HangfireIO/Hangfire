using System.Configuration;
using System.Reflection;
using Sharp.Data;
using Common.Logging;
using Sharp.Migrations;

namespace Hangfire.SharpData {
    public class SharpDataStorage : JobStorage {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharpDataStorage));

        private readonly string _connectionString;
        private readonly string _dataProviderName;

        public SharpDataStorage(string nameOrConnectionString, string dataProviderName) {
            var connectionString = GetConnectionString(nameOrConnectionString);
            _connectionString = connectionString;
            _dataProviderName = dataProviderName;
            CreateOrUpdateDatabase();
        }

        private static string GetConnectionString(string nameOrConnectionString) {
            var connectionString = nameOrConnectionString;
            if (nameOrConnectionString.Contains(";")) {
                connectionString = ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }
            return connectionString;
        }

        private void CreateOrUpdateDatabase() {
            Log.Info("Start installing Hangfire SQL objects...");
            using (var dataClient = SharpFactory.Default.CreateDataClient(_connectionString, _dataProviderName)) {
                var runner = new Runner(dataClient, Assembly.GetExecutingAssembly());
                runner.Run(-1);
            }
            Log.Info("Hangfire Database up to date");
        }

        public override Storage.IMonitoringApi GetMonitoringApi() {
            return new SharpDataMonitoringApi();
        }

        public override Storage.IStorageConnection GetConnection() {
            return new SharpDataStorageConnection(_connectionString, _dataProviderName);
        }

    }
}
