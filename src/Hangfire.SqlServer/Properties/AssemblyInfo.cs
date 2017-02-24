using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Hangfire.SqlServer")]
[assembly: AssemblyDescription("SQL Server job storage for Hangfire")]
[assembly: Guid("3d96bf2f-8854-4872-aee3-faf81d121a4d")]
[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("Hangfire.SqlServer.Tests")]
// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
