using System;
using Hangfire.Server;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ScheduleInstantFactoryFacts
    {
        private readonly CrontabSchedule _crontabSchedule;
        private readonly TimeZoneInfo _timeZone;

        public ScheduleInstantFactoryFacts()
        {
            _crontabSchedule = CrontabSchedule.Parse("* * * * *");
            _timeZone = TimeZoneInfo.Utc;
        }

        [Fact]
        public void GetInstant_ThrowsAnException_WhenCrontabScheduleIsNull()
        {
            var factory = CreateFactory();

            var exception = Assert.Throws<ArgumentNullException>(
                () => factory.GetInstant(null, _timeZone));

            Assert.Equal("schedule", exception.ParamName);
        }

        [Fact]
        public void GetInstant_ThrowsAnException_WhenTimeZoneIsNull()
        {
            var factory = CreateFactory();

            var exception = Assert.Throws<ArgumentNullException>(
                () => factory.GetInstant(_crontabSchedule, null));

            Assert.Equal("timeZone", exception.ParamName);
        }

        [Fact]
        public void GetInstant_ReturnsCorrectlyInitializedInstant()
        {
            var factory = CreateFactory();

            var instant = factory.GetInstant(_crontabSchedule, _timeZone);

            Assert.True(DateTime.UtcNow.AddMinutes(-2) < instant.NowInstant);
            Assert.True(instant.NowInstant < DateTime.UtcNow.AddMinutes(2));
        }

        private static ScheduleInstantFactory CreateFactory()
        {
            return new ScheduleInstantFactory();
        }
    }
}
