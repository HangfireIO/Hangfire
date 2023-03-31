using System.Reflection;
using Hangfire.Common;
using Newtonsoft.Json;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class CleanSerializerSettingsAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            ClearSettings();
        }

        public override void After(MethodInfo methodUnderTest)
        {
            ClearSettings();
        }

        private static void ClearSettings()
        {
#pragma warning disable 618
            JobHelper.SetSerializerSettings(null);
#pragma warning restore 618
            SerializationHelper.SetUserSerializerSettings(null);
#if !NET452 && !NET461
            JsonConvert.DefaultSettings = null;
#endif
        }
    }
}
