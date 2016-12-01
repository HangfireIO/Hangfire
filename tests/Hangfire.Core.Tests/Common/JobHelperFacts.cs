using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Common
{
    public class JobHelperFacts
    {
        private static readonly DateTime WellKnownDateTime = new DateTime(1988, 04, 20, 01, 12, 32, DateTimeKind.Utc);
        private const int WellKnownTimestamp = 577501952;

        [Fact]
        public void ToJson_EncodesNullValueAsNull()
        {
            var result = JobHelper.ToJson(null);
            Assert.Null(result);
        }

        [Fact]
        public void ToJson_EncodesGivenValue_ToJsonString()
        {
            var result = JobHelper.ToJson("hello");
            Assert.Equal("\"hello\"", result);
        }

        [Fact]
        public void FromJson_DecodesNullAsDefaultValue()
        {
            var stringResult = JobHelper.FromJson<string>(null);
            var intResult = JobHelper.FromJson<int>(null);

            Assert.Null(stringResult);
            Assert.Equal(0, intResult);
        }

        [Fact]
        public void FromJson_DecodesFromJsonString()
        {
            var result = JobHelper.FromJson<string>("\"hello\"");
            Assert.Equal("hello", result);
        }

        [Fact]
        public void FromJson_ThrowsAnException_WhenTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => JobHelper.FromJson("1", null));
        }

        [Fact]
        public void FromJson_WithType_DecodesFromJsonString()
        {
            var result = (string) JobHelper.FromJson("\"hello\"", typeof (string));
            Assert.Equal("hello", result);
        }

        [Fact]
        public void FromJson_WithType_DecodesNullValue_ToNull()
        {
            var result = (string) JobHelper.FromJson(null, typeof (string));
            Assert.Null(result);
        }

        [Fact]
        public void ToTimestamp_ReturnsUnixTimestamp_OfTheGivenDateTime()
        {
            var result = JobHelper.ToTimestamp(
                WellKnownDateTime);

            Assert.Equal(WellKnownTimestamp, result);
        }

        [Fact]
        public void ToTimestamp_ReturnsDateTime_ForGivenTimestamp()
        {
            var result = JobHelper.FromTimestamp(WellKnownTimestamp);

            Assert.Equal(WellKnownDateTime, result);
        }

        [Fact]
        public void SerializeDateTime_ReturnsString_InISO8601Format()
        {
            var result = JobHelper.SerializeDateTime(WellKnownDateTime);

            Assert.Equal(WellKnownDateTime.ToString("o"), result);
        }

        [Fact]
        public void DeserializeDateTime_CanDeserialize_Timestamps()
        {
            var result = JobHelper.DeserializeDateTime(WellKnownTimestamp.ToString());

            Assert.Equal(WellKnownDateTime, result);
        }

        [Fact]
        public void DeserializeDateTime_CanDeserialize_ISO8601Format()
        {
            var result = JobHelper.DeserializeDateTime(WellKnownDateTime.ToString("o"));
            Assert.Equal(WellKnownDateTime, result);
        }

        [Fact]
        public void DeserializeNullableDateTime_ReturnsNull_IfNullOrEmptyStringGiven()
        {
            Assert.Null(JobHelper.DeserializeNullableDateTime(""));
            Assert.Null(JobHelper.DeserializeNullableDateTime(null));
        }

        [Fact]
        public void DeserializeNullableDateTime_ReturnsCorrectValue_OnNonNullString()
        {
            var result = JobHelper.DeserializeNullableDateTime(WellKnownTimestamp.ToString());
            Assert.Equal(WellKnownDateTime, result);
        }

        [Fact]
        public void FromJson_WithObjectType_DecodesFromJsonString()
        {
            var result = (ClassA)JobHelper.FromJson(@"{ ""PropertyA"": ""hello"" }", typeof(ClassA));
            Assert.Equal("hello", result.PropertyA);
        }

        [Fact]
        public void ForSerializeUseDefaultConfigurationOfJsonNet()
        {
            var result = JobHelper.ToJson(new ClassA("A"));
            Assert.Equal(@"{""PropertyA"":""A""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ForSerializeCanUseCustomConfigurationOfJsonNet()
        {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                var result = JobHelper.ToJson(new ClassA("A"));
                Assert.Equal(@"{""propertyA"":""A""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ForDeserializeCanUseCustomConfigurationOfJsonNet()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });

            var result =(ClassA)JobHelper.FromJson<IClass>(@"{ ""$type"": ""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"", ""propertyA"":""A"" }");
            Assert.Equal("A", result.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ForDeserializeCanUseCustomConfigurationOfJsonNetWithInvocationData()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            });

            var method = typeof(BackgroundJob).GetMethod("DoWork");
            var args = new object[] { "123", "Test" };
            var job = new Job(typeof(BackgroundJob), method, args);

            var invocationData = InvocationData.Serialize(job);
            var deserializedJob = invocationData.Deserialize();

            Assert.Equal(typeof(BackgroundJob), deserializedJob.Type);
            Assert.Equal(method, deserializedJob.Method);
            Assert.Equal(args, deserializedJob.Args);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ForDeserializeWithGenericMethodCanUseCustomConfigurationOfJsonNet()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,

            });

            var result = (ClassA)JobHelper.FromJson(@"{ ""$type"": ""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"", ""propertyA"":""A"" }", typeof(IClass));
            Assert.Equal("A", result.PropertyA);
        }

        [Fact]
        public void Serialize_ReturnsNull_WnehValueIsNull()
        {
            Assert.Null(JobHelper.Serialize(null));
        }

        [Fact]
        public void Serialize_ReturnsCorrectResult_WhenValueIsString()
        {
            var result = JobHelper.Serialize("Simple string");
            Assert.Equal("\"Simple string\"", result);
        }

        [Fact]
        public void Serialize_ReturnsCorrectValue_WhenValueIsCustomObject()
        {
            var result = JobHelper.Serialize(new ClassA("B"));
            Assert.Equal(@"{""PropertyA"":""B""}", result);
        }

        [Fact]
        public void Serialize_ReturnsCorrectJson_WhenTypeNameHandlingIsSet()
        {
            var result = JobHelper.Serialize(new ClassA("B"), TypeNameHandling.All);
            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""B""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Serialize_HandleJsonDefaultSettingsDoesNotAffect()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Binder = new CustomSerializerBinder(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "ddMMyyyy"
            };

            var result = JobHelper.Serialize(new ClassB { StringValue = "B", DateTimeValue = new DateTime(1961, 4, 12)});
            Assert.Equal(@"{""StringValue"":""B"",""NullValue"":null,""DefaultValue"":0,""DateTimeValue"":""1961-04-12T00:00:00""}", result);
        }

        [Fact]
        public void Deserialize_ReturnsNull_WhenValueIsNull()
        {
            var result = JobHelper.Deserialize<object>(null);
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_ReturnsDefaultValue_WhenGenericArgumentIsValueType()
        {
            var result = JobHelper.Deserialize<int>(null);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Deserialize_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = JobHelper.Deserialize<string>("\"hello\"");
            Assert.Equal("hello", result);
        }

        [Fact]
        public void Deserialize_RetrunsCorrectObject_WhenTypeIsCustomClass()
        {
            var argumentJson = @"{""PropertyA"":""A""}";

            var argumentValue = JobHelper.Deserialize<ClassA>(argumentJson);
            Assert.NotNull(argumentValue);
            Assert.Equal("A", argumentValue.PropertyA);
        }

        [Fact]
        public void Deserialize_RetrunsCorrectObject_WhenTypeNameHandlingIsSet()
        {
            var argumentJson = @"{""PropertyA"":""A""}";

            var argumentValue = JobHelper.Deserialize<ClassA>(argumentJson, TypeNameHandling.All);
            Assert.NotNull(argumentValue);
            Assert.Equal("A", argumentValue.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Deserialize_HandlesUsingArgumentsSerializerSettings_WhenUsingCoreSettingsThrewException()
        {
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Binder = new CustomSerializerBinder()
            });
            var argumentJson = JobHelper.ToJson(new ClassA("A"));

            var argumentValue = JobHelper.Deserialize<ClassA>(argumentJson, TypeNameHandling.All);

            Assert.NotNull(argumentValue);
            Assert.Equal("A", argumentValue.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        public void Deserialize_RethrowsJsonException_WhenValueHasIncorrectFormat()
        {
            var argumentJson = "asdfaljsadkfh";

            Assert.Throws<JsonReaderException>(() => JobHelper.Deserialize<ClassA>(argumentJson));
        }

        private interface IClass
        {
        }

        private class ClassA : IClass
        {
            public ClassA(string propertyA)
            {
                PropertyA = propertyA;
            }

            public string PropertyA { get; }
        }

        private class BackgroundJob
        {
            [UsedImplicitly]
            public void DoWork(string workId, string message)
            {
            }
        }

        private class ClassB
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string StringValue { get; set; }

            // ReSharper disable once UnusedMember.Local
            public object NullValue { get; set; }

            // ReSharper disable once UnusedMember.Local
            public int DefaultValue { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public DateTime? DateTimeValue { get; set; }
        }

        private class CustomSerializerBinder: SerializationBinder
        {
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = "someAssembly";
                typeName = serializedType.FullName.ToUpper();
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(ClassA);
            }
        }

        
    }
}
