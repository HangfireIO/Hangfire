using Hangfire.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class GloabalConfigurationExtensionsFacts
    {
        [Fact, CleanSerializerSettings]
        public void UseSerializationSettings_AffectSerializationWithUserSettings()
        {
            GlobalConfiguration.Configuration.UseSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize(new CustomClass { StringProperty = "Value" }, SerializationOption.User);
            Assert.Equal(@"{""stringProperty"":""Value""}", result);
        }

        [Fact, CleanSerializerSettings]
        public void UseSerializationSettingsWithCallback_AffectSerializationWithUserSettings()
        {
            GlobalConfiguration.Configuration.UseRecommendedSerializerSettings(settings =>
            {
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            var result = SerializationHelper.Serialize(new CustomClass { StringProperty = "Value" }, SerializationOption.User);
            Assert.Equal(@"{""stringProperty"":""Value""}", result);
        }

        public class CustomClass
        {
            public string StringProperty { get; set; }
        }
    }
}
