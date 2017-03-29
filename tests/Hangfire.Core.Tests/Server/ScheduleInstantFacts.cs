using System;
using Cronos;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ScheduleInstantFacts
    {
        private CronExpression _schedule;
        private TimeZoneInfo _timeZone;
        private DateTime _now;

        public ScheduleInstantFacts()
        {
            _now = new DateTime(2012, 12, 12, 12, 12, 0, DateTimeKind.Utc);
            _schedule = CronExpression.Parse("* * * * *");
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

            Assert.Equal("cronExpression", exception.ParamName);
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
            _schedule = CronExpression.Parse("0 * * * *");
            
            var instant = CreateInstant();

            // Act
            var value = instant.NextInstant;

            // Assert
            Assert.Equal(_now.AddHours(1), value);
        }

        [Fact]
        public void ShouldSchedule_DoesntThrow_NearDaylightSavings()
        {
            // Arrange
            _timeZone = GetNewYorkTimeZone();
            _now = TimeZoneInfo.ConvertTime(new DateTime(2016, 3, 13, 3, 0, 0), _timeZone, TimeZoneInfo.Utc);
            _schedule = CronExpression.Parse("0 * * * *");

            var instant = CreateInstant();

            // Act
            var last = TimeZoneInfo.ConvertTime(new DateTime(2016, 3, 13, 1, 0, 0), _timeZone, TimeZoneInfo.Utc);
            var shouldSchedule = instant.ShouldSchedule(last);

            // Assert
            Assert.True(shouldSchedule);
        }

        [Fact]
        public void ShouldSchedule_ThrowsAnException_WhenLastTime_IsNotUtc()
        {
            var instant = CreateInstant();

            Assert.Throws<ArgumentException>(() => instant.ShouldSchedule(DateTime.Now));
        }

        [Fact]
        public void ShouldSchedule_ReturnsTrue_WhenThereAreOccurrencesBetweenNowInstantAndLastInstant()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var shouldSchedule = instant.ShouldSchedule(time.AddMinutes(-3));
            Assert.True(shouldSchedule);
        }

        [Fact]
        public void ShouldSchedule_ReturnsFalse_WhenLastInstantIsNow()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var shouldSchedule = instant.ShouldSchedule(_now);

            // LastInstant should be excluded
            Assert.False(shouldSchedule);
        }

        [Fact]
        public void GetNextInstants_ReturnsFalse_WhenGivenIntervalDoesNotSatisfyTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 01, 00, DateTimeKind.Utc);
            var instant = new ScheduleInstant(time, TimeZoneInfo.Utc, CronExpression.Parse("0 * * * *"));

            var shouldSchedule = instant.ShouldSchedule(time.AddMinutes(50));

            Assert.False(shouldSchedule);
        }

        [Fact]
        public void Factory_ReturnsCorrectlyInitializedInstant()
        {
            var instant = ScheduleInstant.Factory(CronExpression.Parse("* * * * *"), _timeZone);

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
