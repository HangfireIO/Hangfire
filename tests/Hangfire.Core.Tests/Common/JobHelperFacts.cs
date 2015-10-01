using System;
using System.Runtime.Serialization.Formatters;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

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

        [Fact]
        public void ForSerializeCanUseCustomConfigurationOfJsonNet()
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                var result = JobHelper.ToJson(new ClassA("A"));
                Assert.Equal(@"{""propertyA"":""A""}", result);
            }
            finally
            {
                JobHelper.SetSerializerSettings(null);
            }
        }

        [Fact]
        public void ForDeserializeCanUseCustomConfigurationOfJsonNet()
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects
                });

                var result = (ClassA)JobHelper.FromJson<IClass>(@"{ ""$type"": ""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"", ""propertyA"":""A"" }");
                Assert.Equal("A", result.PropertyA);
            }
            finally
            {
                JobHelper.SetSerializerSettings(null);
            }
        }

        [Fact]
        public void ForDeserializeCanUseCustomConfigurationOfJsonNetWithInvocationData()
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                });

                var method = typeof (BackgroundJob).GetMethod("DoWork");
                var args = new object[] { "123", "Test" };
                var job = new Job(typeof(BackgroundJob), method, args);

                var invocationData = InvocationData.Serialize(job);
                var deserializedJob = invocationData.Deserialize();

                Assert.Equal(typeof(BackgroundJob), deserializedJob.Type);
                Assert.Equal(method, deserializedJob.Method);
                Assert.Equal(args, deserializedJob.Args);
            }
            finally
            {
                JobHelper.SetSerializerSettings(null);
            }
        }

        [Fact]
        public void ForDeserializeWithGenericMethodCanUseCustomConfigurationOfJsonNet()
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects
                });

                var result = (ClassA)JobHelper.FromJson(@"{ ""$type"": ""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"", ""propertyA"":""A"" }", typeof(IClass));
                Assert.Equal("A", result.PropertyA);
            }
            finally
            {
                JobHelper.SetSerializerSettings(null);
            }
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

            public string PropertyA { get; private set; }
        }

        private class BackgroundJob
        {
            public void DoWork(string workId, string message)
            {
            }
        }
    }
}
