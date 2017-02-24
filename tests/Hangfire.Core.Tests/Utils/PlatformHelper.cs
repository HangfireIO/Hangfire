using System;

#if !NETFULL
using System.Runtime.InteropServices;
#endif

namespace Hangfire.Core.Tests
{
    internal static class PlatformHelper
    {
        public static bool IsRunningOnWindows()
        {
#if NETFULL
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }
    }
}
