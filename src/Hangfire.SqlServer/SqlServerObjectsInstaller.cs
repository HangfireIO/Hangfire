// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using Dapper;
using Hangfire.Logging;

namespace Hangfire.SqlServer
{
    public static class SqlServerObjectsInstaller
    {
        [Obsolete("This field is unused and will be removed in 2.0.0.")]
        public static readonly int RequiredSchemaVersion = 5;

        public static void Install(DbConnection connection)
        {
            Install(connection, null);
        }

        public static void Install(DbConnection connection, string schema)
        {
            Install(connection, schema, false);
        }

        public static void Install(DbConnection connection, string schema, bool enableHeavyMigrations)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var script = GetInstallScript(schema, enableHeavyMigrations);

            connection.Execute(script, commandTimeout: 0);
        }

        public static string GetInstallScript(string schema, bool enableHeavyMigrations)
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
