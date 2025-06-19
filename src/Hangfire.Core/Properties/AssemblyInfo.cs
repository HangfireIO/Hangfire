using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid("4deecd4f-19f6-426b-aa87-6cd1a03eaa48")]
[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("Hangfire.Core.Tests")]

// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]