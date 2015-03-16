using System;
using System.Linq;
using Hangfire.Server;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ScheduleInstantFacts
    {
        private readonly CrontabSchedule _schedule;
        private readonly TimeZoneInfo _timeZone;
        private DateTime _now;

        public ScheduleInstantFacts()
        {
            _now = new DateTime(2012, 12, 12, 12, 12, 0, DateTimeKind.Utc);
            _schedule = CrontabSchedule.Parse("* * * * *");
            _timeZone = TimeZoneInfo.Utc;
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLocalTimeArgument_HasLocalKind()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ScheduleInstant(new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Local), _timeZone, _schedule));

            Assert.Equal("utcTime", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLocalTimeArgument_HasUnspecifiedKind()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ScheduleInstant(new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Unspecified), _timeZone, _schedule));

            Assert.Equal("utcTime", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenScheduleIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new ScheduleInstant(_now, TimeZoneInfo.Utc, null));

            Assert.Equal("schedule", exception.ParamName);
        }

        [Fact]
        public void NowInstant_ReturnsNormalizedValue()
        {
            _now = new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Utc);
            var instant = CreateInstant();

            var value = instant.NowInstant;

            Assert.Equal(new DateTime(2012, 12, 12, 12, 12, 0, DateTimeKind.Utc), value);
        }

        [Fact]
        public void NextInstant_ReturnsCorrectValue()
        {
            var instant = CreateInstant();

            var value = instant.NextInstant;

            Assert.Equal(_schedule.GetNextOccurrence(_now), value);
        }

        [Fact]
        public void GetNextInstants_ThrowsAnException_WhenLastTime_IsNotUtc()
        {
            var instant = CreateInstant();

            Assert.Throws<ArgumentException>(() => instant.GetNextInstants(DateTime.Now));
        }

        [Fact]
        public void GetNextInstants_ReturnsCollectionOfScheduleMatches_BetweenLocalTime_AndLastMatchingTime()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var matches = instant.GetNextInstants(time.AddMinutes(-3)).ToList();

            Assert.Equal(3, matches.Count);
            Assert.Equal(time.AddMinutes(-2), matches[0]);
            Assert.Equal(time.AddMinutes(-1), matches[1]);
            Assert.Equal(time, matches[2]);
        }

        [Fact]
        public void GetNextInstants_ReturnsSingleMatch_WhenLocalTimeSatisfiesTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var matches = instant.GetNextInstants(null).ToList();

            Assert.Equal(1, matches.Count);
            Assert.Equal(time, matches[0]);
        }

        [Fact]
        public void GetNextInstants_ReturnsEmptyCollection_WhenGivenIntervalDoesNotSatisfyTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 01, 00, DateTimeKind.Utc);
            var instant = new ScheduleInstant(time, TimeZoneInfo.Utc, CrontabSchedule.Parse("0 * * * *"));

            var matches = instant.GetNextInstants(time.AddMinutes(50));

            Assert.Empty(matches);
        }

        private ScheduleInstant CreateInstant(DateTime? localTime = null)
        {
            return new ScheduleInstant(localTime ?? _now, TimeZoneInfo.Utc, _schedule);
        }
    }
}
