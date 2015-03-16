using System;
using Hangfire.Server;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ScheduleInstantFactoryFacts
    {
        [Fact]
        public void GetInstant_ReturnsCorrectlyInitializedInstant()
        {
            var factory = new ScheduleInstantFactory();
            var schedule = CrontabSchedule.Parse("* * * * *");

            IScheduleInstant instant = null;
            Assert.DoesNotThrow(() => instant = factory.GetInstant(schedule, TimeZoneInfo.Utc));

            Assert.NotNull(instant);
        }
    }
}
