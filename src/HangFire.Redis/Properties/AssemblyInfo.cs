using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Hangfire.Redis")]
[assembly: AssemblyDescription("Redis job storage implementation.")]
[assembly: Guid("68ebd93a-5138-434a-ba9f-b359236c980f")]
[assembly: InternalsVisibleTo("Hangfire.Redis.Tests")]
[assembly: InternalsVisibleTo("Hangfire.Tests")]

// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]