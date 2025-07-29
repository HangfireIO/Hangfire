// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Data.Common;
using System.IO;
using System.Reflection;
using Hangfire.Annotations;

namespace Hangfire.SqlServer
{
    public static class SqlServerObjectsInstaller
    {
        [Obsolete("This field is unused and will be removed in 2.0.0.")]
        public static readonly int RequiredSchemaVersion = 5;

        public static readonly int LatestSchemaVersion = 9;

        public static void Install([NotNull] DbConnection connection)
        {
            Install(connection, null);
        }

        public static void Install([NotNull] DbConnection connection, [CanBeNull] string? schema)
        {
            Install(connection, schema, false);
        }

        public static void Install([NotNull] DbConnection connection, [CanBeNull] string? schema, bool enableHeavyMigrations)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var script = GetInstallScript(schema, enableHeavyMigrations);
            var connectionWasClosed = connection.State == ConnectionState.Closed;

            if (connectionWasClosed)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand(script, timeout: 0);
                command.ExecuteNonQuery();
            }
            finally
            {
                if (connectionWasClosed)
                {
                    connection.Close();
                }
            }
        }

        public static string GetInstallScript([CanBeNull] string? schema, bool enableHeavyMigrations)
        {
            var script = GetStringResource(
                typeof(SqlServerObjectsInstaller).GetTypeInfo().Assembly,
                "Hangfire.SqlServer.Install.sql");

            script = script.Replace("$(HangFireSchema)", !string.IsNullOrWhiteSpace(schema) ? schema : Constants.DefaultSchema);

            if (!enableHeavyMigrations)
            {
                script = script.Replace("--SET @DISABLE_HEAVY_MIGRATIONS = 1;", "SET @DISABLE_HEAVY_MIGRATIONS = 1;");
            }

            return script;
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
