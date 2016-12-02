using Hangfire.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class GloabalConfigurationExtensionsFacts
    {
        [Fact]
        public void UseSerializationSettings_AffectSerializatonWithUserSettings()
        {
            GlobalConfiguration.Configuration.UseSerializationSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = SerializationHelper.Serialize(new CustomClass {StringProperty = "Value"}, SerializationOption.User);
            Assert.Equal(@"{""stringProperty"":""Value""}", result);
        }

        public class CustomClass
        {
            public string StringProperty { get; set; }
        }
    }
}