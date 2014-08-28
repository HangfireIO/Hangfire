using System;
using System.Linq;
using Hangfire.Server;
using Moq;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ScheduleInstantFacts
    {
        private readonly CrontabSchedule _schedule;
        private readonly DateTime _localTime;

        public ScheduleInstantFacts()
        {
            _localTime = new DateTime(2012, 12, 12, 12, 12, 0, DateTimeKind.Local);
            _schedule = CrontabSchedule.Parse("* * * * *");
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenLocalTimeArgument_HasUtcKind()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ScheduleInstant(DateTime.UtcNow, _schedule));

            Assert.Equal("localTime", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenScheduleIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new ScheduleInstant(_localTime, null));

            Assert.Equal("schedule", exception.ParamName);
        }

        [Fact]
        public void LocalTime_ReturnsCorrectValue()
        {
            var instant = CreateInstant();

            var value = instant.LocalTime;

            Assert.Equal(_localTime, value);
        }

        [Fact]
        public void NextOccurrence_ReturnsCorrectValue()
        {
            var instant = CreateInstant();

            var value = instant.NextOccurrence;

            Assert.Equal(_schedule.GetNextOccurrence(_localTime), value);
        }

        [Fact]
        public void GetMatches_ThrowsAnException_WhenLastTime_IsNotLocal()
        {
            var instant = CreateInstant();

            Assert.Throws<ArgumentException>(() => instant.GetMatches(DateTime.UtcNow));
        }

        [Fact]
        public void GetMatches_ReturnsCollectionOfScheduleMatches_BetweenLocalTime_AndLastMatchingTime()
        {
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Local);
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
            var time = new DateTime(2012, 12, 12, 00, 00, 00, DateTimeKind.Local);
            var instant = CreateInstant(time);

            var matches = instant.GetMatches(null).ToList();

            Assert.Equal(1, matches.Count);
            Assert.Equal(time, matches[0]);
        }

        [Fact]
        public void GetMatches_ReturnsEmptyCollection_WhenGivenIntervalDoesNotSatisfyTheSchedule()
        {
            var time = new DateTime(2012, 12, 12, 00, 01, 00, DateTimeKind.Local);
            var instant = new ScheduleInstant(time, CrontabSchedule.Parse("0 * * * *"));

            var matches = instant.GetMatches(time.AddMinutes(50));

            Assert.Empty(matches);
        }

        private ScheduleInstant CreateInstant(DateTime? localTime = null)
        {
            return new ScheduleInstant(localTime ?? _localTime, _schedule);
        }
    }
}
