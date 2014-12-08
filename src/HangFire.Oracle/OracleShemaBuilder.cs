// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System.Data;
using System.Reflection;
using Hangfire.Sql;
using Sharp.Data;
using Sharp.Data.Databases;
using Sharp.Migrations;

namespace Hangfire.Oracle
{
    public class OracleShemaBuilder : ISchemaBuilder
    {
        private readonly string _connectionString;

        public OracleShemaBuilder(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void BuildDatabase(IDbConnection connection)
        {
            var client = SharpFactory.Default.CreateDataClient(_connectionString, DataProviderNames.OracleManaged);
            var runner = new Runner(client, Assembly.GetExecutingAssembly());
            runner.Run(-1);
        }
    }
}