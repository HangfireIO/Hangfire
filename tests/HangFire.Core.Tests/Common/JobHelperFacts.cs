using System;
using HangFire.Common;
using Xunit;

namespace HangFire.Core.Tests.Common
{
    public class JobHelperFacts
    {
        private DateTime _wellKnownDateTime = new DateTime(1988, 04, 20, 01, 12, 32, DateTimeKind.Utc);
        private int _wellKnownTimestamp = 577501952;

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
        public void ToTimestamp_ReturnsUnixTimestamp_OfTheGivenDateTime()
        {
            var result = JobHelper.ToTimestamp(
                _wellKnownDateTime);

            Assert.Equal(_wellKnownTimestamp, result);
        }

        [Fact]
        public void ToTimestamp_ReturnsDateTime_ForGivenTimestamp()
        {
            var result = JobHelper.FromTimestamp(_wellKnownTimestamp);

            Assert.Equal(_wellKnownDateTime, result);
        }

        [Fact]
        public void ToStringTimestamp_ReturnsCorrectValue()
        {
            var result = JobHelper.ToStringTimestamp(_wellKnownDateTime);

            Assert.Equal(_wellKnownTimestamp.ToString(), result);
        }

        [Fact]
        public void FromStringTimestamp_ReturnsCorrectValue()
        {
            var result = JobHelper.FromStringTimestamp(_wellKnownTimestamp.ToString());

            Assert.Equal(_wellKnownDateTime, result);
        }

        [Fact]
        public void FromNullableStringTimestamp_ReturnsNull_IfNullOrEmptyStringGiven()
        {
            Assert.Null(JobHelper.FromNullableStringTimestamp(""));
            Assert.Null(JobHelper.FromNullableStringTimestamp(null));
        }

        [Fact]
        public void FromNullableStringTimestamp_ReturnsCorrectValue_OnNonNullString()
        {
            var result = JobHelper.FromNullableStringTimestamp(_wellKnownTimestamp.ToString());
            Assert.Equal(_wellKnownDateTime, result);
        }
    }
}
