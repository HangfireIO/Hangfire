﻿using System.Reflection;
using Hangfire.Common;
using Newtonsoft.Json;
using Xunit.Sdk;

namespace Hangfire.SqlServer.Tests
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
            GlobalConfiguration.Configuration.UseSerializerSettings(null);
            JsonConvert.DefaultSettings = null;
        }
    }
}
