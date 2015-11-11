using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServer2005Facts
    {
        [Fact]
        public void Ctor_CreatesDefaultSettings()
        {
            var options = new SqlServerStorageOptions();
            var storage = new SqlServerStorage(ConnectionUtils.GetConnectionString(), options);
            Assert.Equal(typeof(SqlServerDefaultSettings), storage.SqlServerSettings.GetType());
        }

        [Fact]
        public void Ctor_CreatesSqlServer2005Settings()
        {
            var options = new SqlServerStorageOptions
            {
                SqlServer2005Compatibility = true
            };
            var storage = new SqlServerStorage(ConnectionUtils.GetConnectionString(), options);
            Assert.Equal(typeof(SqlServer2005Settings), storage.SqlServerSettings.GetType());
        }

        [Fact]
        public void TransformScript_RemovesDateTime2()
        {
            var script = GetStringResource(
                typeof(SqlServerObjectsInstaller).Assembly,
                "Hangfire.SqlServer.Install.sql");

            Assert.False(new SqlServer2005Settings().TransformScript(script).Contains("datetime2"));
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

    }
}