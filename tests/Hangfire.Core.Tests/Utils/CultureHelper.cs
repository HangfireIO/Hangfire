using System.Globalization;
using System.Threading;

namespace Hangfire.Core.Tests
{
    internal static class CultureHelper
    {
        public static void SetCurrentCulture(CultureInfo cultureInfo)
        {
#if !NETCOREAPP1_0
            Thread.CurrentThread.CurrentCulture = cultureInfo;
#else
            CultureInfo.CurrentCulture = cultureInfo;
#endif
        }

        public static void SetCurrentUICulture(CultureInfo cultureInfo)
        {
#if !NETCOREAPP1_0
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
#else
            CultureInfo.CurrentUICulture = cultureInfo;
#endif
        }

        public static void SetCurrentCulture(string id)
        {
#if !NETCOREAPP1_0
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(id);
#else
            CultureInfo.CurrentCulture = new CultureInfo(id);
#endif
        }

        public static void SetCurrentUICulture(string id)
        {
#if !NETCOREAPP1_0
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(id);
#else
            CultureInfo.CurrentUICulture = new CultureInfo(id);
#endif
        }
    }
}
