// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using Hangfire.Sql;
using Hangfire.Logging;

namespace Hangfire.SqlServer
{
    [ExcludeFromCodeCoverage]
    public class SqlServerObjectsInstaller : ISchemaBuilder
    {
        private const int RequiredSchemaVersion = 3;

        private static readonly ILog Log = LogProvider.GetLogger(typeof(SqlServerStorage));

        public void BuildDatabase(IDbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            Log.Info("Start installing Hangfire SQL objects...");

            if (!IsSqlEditionSupported(connection))
            {
                throw new PlatformNotSupportedException("The SQL Server edition of the target server is unsupported, e.g. SQL Azure.");
            }

            var script = GetStringResource(
                typeof(SqlServerObjectsInstaller).Assembly, 
                "Hangfire.SqlServer.Install.sql");

            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 3;", "SET @TARGET_SCHEMA_VERSION = " + RequiredSchemaVersion + ";");

            connection.Execute(script);

            Log.Info("Hangfire SQL objects installed.");
        }

        private static bool IsSqlEditionSupported(IDbConnection connection)
        {
            var edition = connection.Query<int>("SELECT SERVERPROPERTY ( 'EngineEdition' )").Single();
            return edition >= SqlEngineEdition.Standard && edition <= SqlEngineEdition.SqlAzure;
        }

        private static string GetStringResource(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) 
                {
                    throw new InvalidOperationException(String.Format(
                        "Requested resource `{0}` was not found in the assembly `{1}`.",
                        resourceName,
                        assembly));
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static class SqlEngineEdition
        {
// ReSharper disable UnusedMember.Local
            // See article http://technet.microsoft.com/en-us/library/ms174396.aspx for details on EngineEdition
            public const int Personal = 1;
            public const int Standard = 2;
            public const int Enterprise = 3;
            public const int Express = 4;
            public const int SqlAzure = 5;
// ReSharper restore UnusedMember.Local
        }

       
    }
}
