using System;
using System.Reflection;
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
        public void ArgumentToJson_ReturnsNull_WhenValueIsNull()
        {
            var result = JobHelper.ArgumentToJson(null);
            Assert.Null(result);
        }

        [Fact]
        public void ArgumentToJson_ReturnCorrectResult_WhenValueIsString()
        {
            var result = JobHelper.ArgumentToJson("Simple string");
            Assert.Equal("\"Simple string\"", result);
        }

        [Fact]
        public void ArgumentToJson_ReturnsCorrectValue_WhenValueIsCustomObject()
        {
            var result = JobHelper.ArgumentToJson(new ClassA("B"));
            Assert.Equal(@"{""PropertyA"":""B""}", result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ArgumentToJson_ReturnCorrectValue_WhenArgumentsSerializerSettingsIsDefined()
        {
            JobHelper.SetArgumentsSerializerSettings(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.All
            });

            var result = JobHelper.ArgumentToJson(new ClassA("A"));
            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"",""propertyA"":""A""}", result);
        }

        [Fact]
        public void ArgumentFromJson_ThrowsAnException_WhenTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => JobHelper.ArgumentFromJson("1", null));
        }

        [Fact]
        public void ArgumentFromJson_ReturnsCorrectValue_WhenValueIsString()
        {
            var result = (string)JobHelper.ArgumentFromJson("\"hello\"", typeof(string));
            Assert.Equal("hello", result);
        }

        [Fact]
        public void ArgumentFromJson_ReturnsNull_WhenValueIsNull()
        {
            var result = (string)JobHelper.ArgumentFromJson(null, typeof(string));
            Assert.Null(result);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ArgumentFromJson_RetrunsObject_WhenTypeIsCustomClass()
        {
            var argumentJson = @"{""PropertyA"":""A""}";

            var argumentValue = JobHelper.ArgumentFromJson(argumentJson, typeof(ClassA)) as ClassA;
            Assert.NotNull(argumentValue);
            Assert.Equal("A", argumentValue.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        public void ArgumentFromJson_ReturnsObject_WhenTypeIsInterface()
        {
            JobHelper.SetArgumentsSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            var argumentJson = @"{""$type"":""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""A""}";

            var argumentValue = JobHelper.ArgumentFromJson(argumentJson, typeof(IClass)) as ClassA;
            Assert.NotNull(argumentValue);
            Assert.Equal("A", argumentValue.PropertyA);
        }

        [Fact, CleanJsonSerializersSettings]
        public void SetSerializerSettings_SetsBothArgumentAndCoreSerializerSettings()
        {
#pragma warning disable 618
            JobHelper.SetSerializerSettings(new JsonSerializerSettings
#pragma warning restore 618
            {
                TypeNameHandling = TypeNameHandling.All
            });

            var coreSerializerResult = JobHelper.ToJson(new ClassA("A"));
            var argumentsSerializerResult = JobHelper.ArgumentToJson(new ClassA("A"));
            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""A""}", coreSerializerResult);
            Assert.Equal(@"{""$type"":""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"",""PropertyA"":""A""}", argumentsSerializerResult);
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
    }
}
