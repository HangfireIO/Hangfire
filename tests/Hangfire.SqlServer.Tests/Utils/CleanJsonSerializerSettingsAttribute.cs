using System.Reflection;
using Newtonsoft.Json;
using Xunit.Sdk;

namespace Hangfire.SqlServer.Tests
{
    internal class CleanJsonSerializersSettingsAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
            GlobalConfiguration.Configuration.UseSerializationSettings(null);
            JsonConvert.DefaultSettings = null;
        }
    }
}