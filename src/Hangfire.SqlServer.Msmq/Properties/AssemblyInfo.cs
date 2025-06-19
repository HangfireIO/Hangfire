using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid("03092b5c-0dfc-4c6c-8422-556bd1cb291e")]
[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("Hangfire.SqlServer.Msmq.Tests")]
// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]