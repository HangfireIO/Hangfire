using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Hangfire.SqlServer.RabbitMq")]
[assembly: AssemblyDescription("Hangfire RabbitMQ job queue for SQL Server storage implementation")]
[assembly: Guid("cb0b1993-ef0a-4cfa-a1f0-a670dde3c12c")]

[assembly: InternalsVisibleTo("Hangfire.SqlServer.RabbitMq.Tests")]
// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]