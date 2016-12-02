using System.Reflection;
using Hangfire.Common;
using Newtonsoft.Json;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class CleanJsonSerializersSettingsAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
#pragma warning disable 618
            JobHelper.SetSerializerSettings(null);
#pragma warning restore 618
            SerializationHelper.SetUserSerializerSettings(null);
            JsonConvert.DefaultSettings = null;
        }
    }
}