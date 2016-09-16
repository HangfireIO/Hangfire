using System;
using System.Linq;
using Hangfire.Server;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ScheduleInstantFacts
    {
        private CrontabSchedule _schedule;
        private TimeZoneInfo _timeZone;
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

            Assert.Equal("nowInstant", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLocalTimeArgument_HasUnspecifiedKind()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ScheduleInstant(new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Unspecified), _timeZone, _schedule));

            Assert.Equal("nowInstant", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenScheduleIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new ScheduleInstant(_now, _timeZone, null));

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
        public void NextInstant_DoesntThrow_NearDaylightSavings()
        {
            // Arrange
            _timeZone = GetNewYorkTimeZone();
            _now = TimeZoneInfo.ConvertTime(new DateTime(2016, 3, 13, 1, 0, 0), _timeZone, TimeZoneInfo.Utc);
            _schedule = CrontabSchedule.Parse("0 * * * *");
            
            var instant = CreateInstant();

            // Act
            var value = instant.NextInstant;

            // Assert
            Assert.Equal(_now.AddHours(1), value);
        }

        [Fact]
        public void GetNextInstants_DoesntThrow_NearDaylightSavings()
        {
            // Arrange
            _timeZone = GetNewYorkTimeZone();
            _now = TimeZoneInfo.ConvertTime(new DateTime(2016, 3, 13, 3, 0, 0), _timeZone, TimeZoneInfo.Utc);
            _schedule = CrontabSchedule.Parse("0 * * * *");

            var instant = CreateInstant();

            // Act
            var last = TimeZoneInfo.ConvertTime(new DateTime(2016, 3, 13, 1, 0, 0), _timeZone, TimeZoneInfo.Utc);
            var value = instant.GetNextInstants(last).ToList();

            // Assert
            Assert.Equal(1, value.Count);
            Assert.Equal(last.AddHours(1), value[0]);
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
        public void GetNextInstants_ReturnsEmptyCollection_WhenLastInstantIsNow()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var matches = instant.GetNextInstants(_now).ToList();

            // LastInstant should be excluded
            Assert.Equal(0, matches.Count);
        }

        [Fact]
        public void GetNextInstants_ReturnsEmptyCollection_WhenGivenIntervalDoesNotSatisfyTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 01, 00, DateTimeKind.Utc);
            var instant = new ScheduleInstant(time, TimeZoneInfo.Utc, CrontabSchedule.Parse("0 * * * *"));

            var matches = instant.GetNextInstants(time.AddMinutes(50));

            Assert.Empty(matches);
        }

        [Fact]
        public void Factory_ReturnsCorrectlyInitializedInstant()
        {
            var instant = ScheduleInstant.Factory(CrontabSchedule.Parse("* * * * *"), _timeZone);

            Assert.True(DateTime.UtcNow.AddMinutes(-2) < instant.NowInstant);
            Assert.True(instant.NowInstant < DateTime.UtcNow.AddMinutes(2));
        }

        private ScheduleInstant CreateInstant(DateTime? localTime = null)
        {
            return new ScheduleInstant(localTime ?? _now, _timeZone, _schedule);
        }

        private static TimeZoneInfo GetNewYorkTimeZone()
        {
            var timeZoneId = PlatformHelper.IsRunningOnWindows() ? "Eastern Standard Time" : "America/New_York";

            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
    }
}
