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
            JobHelper.SetSerializerSettings(null);
            JsonConvert.DefaultSettings = null;
        }
    }
}