using System.Globalization;
using System.Threading;

namespace Hangfire.Core.Tests
{
    internal static class CultureHelper
    {
        public static void SetCurrentCulture(string id)
        {
#if NETFULL
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(id);
#else
            CultureInfo.CurrentCulture = new CultureInfo(id);
#endif
        }

        public static void SetCurrentUICulture(string id)
        {
#if NETFULL
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(id);
#else
            CultureInfo.CurrentUICulture = new CultureInfo(id);
#endif
        }
    }
}
