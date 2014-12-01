using System.Data;
using System.Reflection;
using Hangfire.Sql;
using Sharp.Data;
using Sharp.Data.Databases;
using Sharp.Migrations;

namespace Hangfire.Oracle {
    public class OracleShemaBuilder : ISchemaBuilder {
        private readonly string _connectionString;

        public OracleShemaBuilder(string connectionString) {
            _connectionString = connectionString;
        }

        public void BuildDatabase(IDbConnection connection) {
            var client = SharpFactory.Default.CreateDataClient(_connectionString, DataProviderNames.OracleManaged);
            var runner = new Runner(client, Assembly.GetExecutingAssembly());
            runner.Run(-1);
        }
    }
}