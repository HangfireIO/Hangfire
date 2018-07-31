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

        public static void Install(DbConnection connection)
        {
            Install(connection, null);
        }

        public static void Install(DbConnection connection, string schema)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var log = LogProvider.GetLogger(typeof(SqlServerObjectsInstaller));

            log.Info("Start installing Hangfire SQL objects...");

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
                        log.WarnException("Deadlock occurred during automatic migration execution. Retrying...", ex);
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

            log.Info("Hangfire SQL objects installed.");
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
    }
}
