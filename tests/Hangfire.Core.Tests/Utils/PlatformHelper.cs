using System;

#if NETCOREAPP1_0
using System.Runtime.InteropServices;
#endif

namespace Hangfire.Core.Tests
{
    internal static class PlatformHelper
    {
        public static bool IsRunningOnWindows()
        {
#if !NETCOREAPP1_0
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }
    }
}
