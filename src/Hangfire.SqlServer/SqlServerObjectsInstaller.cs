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
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using Hangfire.Logging;

namespace Hangfire.SqlServer
{
    public static class SqlServerObjectsInstaller
    {
        public static readonly int RequiredSchemaVersion = 5;
        private const int RetryAttempts = 3;

        private static readonly ILog Log = LogProvider.GetLogger(typeof(SqlServerStorage));

        public static void Install(DbConnection connection)
        {
            Install(connection, null);
        }

        public static void Install(DbConnection connection, string schema)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            Log.Info("Start installing Hangfire SQL objects...");

            if (!IsSqlEditionSupported(connection))
            {
                throw new PlatformNotSupportedException("The SQL Server edition of the target server is unsupported, e.g. SQL Azure.");
            }

            var script = GetStringResource(
                typeof(SqlServerObjectsInstaller).GetTypeInfo().Assembly, 
                "Hangfire.SqlServer.Install.sql");

            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 5;", "SET @TARGET_SCHEMA_VERSION = " + RequiredSchemaVersion + ";");

            script = script.Replace("$(HangFireSchema)", !string.IsNullOrWhiteSpace(schema) ? schema : Constants.DefaultSchema);

#if NETFULL
            for (var i = 0; i < RetryAttempts; i++)
            {
                try
                {
                    connection.Execute(script, commandTimeout: 0);
                    break;
                }
                catch (DbException ex)
                {
                    if (ex.ErrorCode == 1205)
                    {
                        Log.WarnException("Deadlock occurred during automatic migration execution. Retrying...", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
#else
            connection.Execute(script, commandTimeout: 0);
#endif

            Log.Info("Hangfire SQL objects installed.");
        }

        private static bool IsSqlEditionSupported(DbConnection connection)
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
                    throw new InvalidOperationException(
                        $"Requested resource `{resourceName}` was not found in the assembly `{assembly}`.");
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
