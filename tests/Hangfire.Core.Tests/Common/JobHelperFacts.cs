using System;
#if NETCOREAPP1_0
using System.Reflection;
#endif
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
#pragma warning disable 618

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Common
{
    public class JobHelperFacts
    {
        private static readonly DateTime WellKnownDateTime = new DateTime(1988, 04, 20, 01, 12, 32, DateTimeKind.Utc);
        private const int WellKnownTimestamp = 577501952;
        private const long WellKnownMillisecondTimestamp = 577501952000;

        [DataCompatibilityRangeFact]
        public void ToJson_EncodesNullValueAsNull()
        {
            var result = JobHelper.ToJson(null);
            Assert.Null(result);
        }

        [DataCompatibilityRangeFact]
        public void ToJson_EncodesGivenValue_ToJsonString()
        {
            var result = JobHelper.ToJson("hello");
            Assert.Equal("\"hello\"", result);
        }

        [DataCompatibilityRangeFact]
        public void FromJson_DecodesNullAsDefaultValue()
        {
            var stringResult = JobHelper.FromJson<string>(null);
            var intResult = JobHelper.FromJson<int>(null);

            Assert.Null(stringResult);
            Assert.Equal(0, intResult);
        }

        [DataCompatibilityRangeFact]
        public void FromJson_DecodesFromJsonString()
        {
            var result = JobHelper.FromJson<string>("\"hello\"");
            Assert.Equal("hello", result);
        }

        [DataCompatibilityRangeFact]
        public void FromJson_ThrowsAnException_WhenTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => JobHelper.FromJson("1", null));
        }

        [DataCompatibilityRangeFact]
        public void FromJson_WithType_DecodesFromJsonString()
        {
            var result = (string) JobHelper.FromJson("\"hello\"", typeof (string));
            Assert.Equal("hello", result);
        }

        [DataCompatibilityRangeFact]
        public void FromJson_WithType_DecodesNullValue_ToNull()
        {
            var result = (string) JobHelper.FromJson(null, typeof (string));
            Assert.Null(result);
        }

        [DataCompatibilityRangeFact]
        public void ToTimestamp_ReturnsUnixTimestamp_OfTheGivenDateTime()
        {
            var result = JobHelper.ToTimestamp(
                WellKnownDateTime);

            Assert.Equal(WellKnownTimestamp, result);
        }

        [DataCompatibilityRangeFact]
        public void FromTimestamp_ReturnsDateTime_ForGivenTimestamp()
        {
            var result = JobHelper.FromTimestamp(WellKnownTimestamp);

            Assert.Equal(WellKnownDateTime, result);
        }

        [DataCompatibilityRangeFact]
        public void ToMillisecondTimestamp_ReturnsTheCorrectResult()
        {
            var result = JobHelper.ToMillisecondTimestamp(WellKnownDateTime);
            Assert.Equal(WellKnownMillisecondTimestamp, result);
        }

        [DataCompatibilityRangeFact]
        public void FromMillisecondTimestamp_ReturnsTheCorrectDateTime()
        {
            var result = JobHelper.FromMillisecondTimestamp(WellKnownMillisecondTimestamp);
            Assert.Equal(WellKnownDateTime, result);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsString_InISO8601Format_In_Version_Pre_170()
        {
            var result = JobHelper.SerializeDateTime(WellKnownDateTime);

            Assert.Equal(WellKnownDateTime.ToString("O"), result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsString_WithMillisecondTimestamp_In_Version_170()
        {
            var result = JobHelper.SerializeDateTime(WellKnownDateTime);

            Assert.Equal(WellKnownMillisecondTimestamp.ToString(), result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsMillisecondTimestamp_ForRecentDates()
        {
            var dateTime = DateTime.UtcNow;
            var result = JobHelper.SerializeDateTime(dateTime);

            Assert.True(long.TryParse(result, out var timestamp));
            Assert.Equal(JobHelper.ToMillisecondTimestamp(dateTime), timestamp);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsMillisecondTimestamp_AtLeastUpTo2100()
        {
            var dateTime = new DateTime(2100, 01, 01, 00, 00, 00, DateTimeKind.Utc);
            var result = JobHelper.SerializeDateTime(dateTime);

            Assert.True(long.TryParse(result, out var timestamp));
            Assert.Equal(JobHelper.ToMillisecondTimestamp(dateTime), timestamp);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsISO8601String_ForConflictingRanges_WithSecondBasedTimestamps()
        {
            var dateTime = new DateTime(1975, 01, 01, 00, 00, 00, DateTimeKind.Utc);
            var result = JobHelper.SerializeDateTime(dateTime);

            Assert.False(long.TryParse(result, out _));
            Assert.Equal(dateTime.ToString("O"), result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsISO8601String_WhenTimestampIsNotApplicable()
        {
            var dateTime = new DateTime(1900, 01, 01, 00, 00, 00, DateTimeKind.Utc);
            var result = JobHelper.SerializeDateTime(dateTime);

            Assert.False(long.TryParse(result, out _));
            Assert.Equal(dateTime.ToString("O"), result);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeDateTime_ReturnsISO8601String_WithMaxDateTime()
        {
            var dateTime = DateTime.MaxValue;
            var result = JobHelper.SerializeDateTime(dateTime);

            Assert.False(long.TryParse(result, out _));
            Assert.Equal(dateTime.ToString("O"), result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeDateTime_CanDeserialize_Timestamps()
        {
            var result = JobHelper.DeserializeDateTime(WellKnownTimestamp.ToString());

            Assert.Equal(WellKnownDateTime, result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeDateTime_CanDeserialize_ISO8601Format()
        {
            var result = JobHelper.DeserializeDateTime(WellKnownDateTime.ToString("o"));
            Assert.Equal(WellKnownDateTime, result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeDateTime_CanDeserialize_MillisecondTimestamp()
        {
            var result = JobHelper.DeserializeDateTime(WellKnownMillisecondTimestamp.ToString());
            Assert.Equal(WellKnownDateTime, result);
        }

        [DataCompatibilityRangeFact]
        public void DeserializeNullableDateTime_ReturnsNull_IfNullOrEmptyStringGiven()
        {
            Assert.Null(JobHelper.DeserializeNullableDateTime(""));
            Assert.Null(JobHelper.DeserializeNullableDateTime(null));
        }

        [DataCompatibilityRangeFact]
        public void DeserializeNullableDateTime_ReturnsCorrectValue_OnNonNullString()
        {
            var result = JobHelper.DeserializeNullableDateTime(WellKnownTimestamp.ToString());
            Assert.Equal(WellKnownDateTime, result);
        }

        [DataCompatibilityRangeFact]
        public void FromJson_WithObjectType_DecodesFromJsonString()
        {
            var result = (ClassA)JobHelper.FromJson(@"{ ""PropertyA"": ""hello"" }", typeof(ClassA));
            Assert.NotNull(result);
            Assert.Equal("hello", result.PropertyA);
        }

        [DataCompatibilityRangeFact]
        public void ForSerializeUseDefaultConfigurationOfJsonNet()
        {
            var result = JobHelper.ToJson(new ClassA("A"));
            Assert.Equal(@"{""PropertyA"":""A""}", result);
        }

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
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

        [DataCompatibilityRangeFact]
        public void ForDeserializeCanUseCustomConfigurationOfJsonNetWithInvocationData()
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
#if !NET452 && !NET461 && !NETCOREAPP1_0
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
#endif
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

        [DataCompatibilityRangeFact]
        public void ForDeserializeWithGenericMethodCanUseCustomConfigurationOfJsonNet()
        {
            try
            {
                JobHelper.SetSerializerSettings(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects
                });

                var result = (ClassA)JobHelper.FromJson(@"{ ""$type"": ""Hangfire.Core.Tests.Common.JobHelperFacts+ClassA, Hangfire.Core.Tests"", ""propertyA"":""A"" }", typeof(IClass));

                Assert.NotNull(result);
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
