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
        private DateTime _utcTime;

        public ScheduleInstantFacts()
        {
            _utcTime = new DateTime(2012, 12, 12, 12, 12, 0, DateTimeKind.Utc);
            _schedule = CrontabSchedule.Parse("* * * * *");
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLocalTimeArgument_HasLocalKind()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ScheduleInstant(new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Local), _schedule));

            Assert.Equal("utcTime", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLocalTimeArgument_HasUnspecifiedKind()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ScheduleInstant(new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Unspecified), _schedule));

            Assert.Equal("utcTime", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenScheduleIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new ScheduleInstant(_utcTime, null));

            Assert.Equal("schedule", exception.ParamName);
        }

        [Fact]
        public void UtcTime_ReturnsNormalizedValue()
        {
            _utcTime = new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Utc);
            var instant = CreateInstant();

            var value = instant.UtcTime;

            Assert.Equal(new DateTime(2012, 12, 12, 12, 12, 0, DateTimeKind.Utc), value);
        }

        [Fact]
        public void NextOccurrence_ReturnsCorrectValue()
        {
            var instant = CreateInstant();

            var value = instant.NextOccurrence;

            Assert.Equal(_schedule.GetNextOccurrence(_utcTime), value);
        }

        [Fact]
        public void GetMatches_ThrowsAnException_WhenLastTime_IsNotUtc()
        {
            var instant = CreateInstant();

            Assert.Throws<ArgumentException>(() => instant.GetMatches(DateTime.Now));
        }

        [Fact]
        public void GetMatches_ReturnsCollectionOfScheduleMatches_BetweenLocalTime_AndLastMatchingTime()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var matches = instant.GetMatches(time.AddMinutes(-3)).ToList();

            Assert.Equal(3, matches.Count);
            Assert.Equal(time.AddMinutes(-2), matches[0]);
            Assert.Equal(time.AddMinutes(-1), matches[1]);
            Assert.Equal(time, matches[2]);
        }

        [Fact]
        public void GetMatches_ReturnsSingleMatch_WhenLocalTimeSatisfiesTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Utc);
            var instant = CreateInstant(time);

            var matches = instant.GetMatches(null).ToList();

            Assert.Equal(1, matches.Count);
            Assert.Equal(time, matches[0]);
        }

        [Fact]
        public void GetMatches_ReturnsEmptyCollection_WhenGivenIntervalDoesNotSatisfyTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 01, 00, DateTimeKind.Utc);
            var instant = new ScheduleInstant(time, CrontabSchedule.Parse("0 * * * *"));

            var matches = instant.GetMatches(time.AddMinutes(50));

            Assert.Empty(matches);
        }

        private ScheduleInstant CreateInstant(DateTime? localTime = null)
        {
            return new ScheduleInstant(localTime ?? _utcTime, _schedule);
        }
    }
}
