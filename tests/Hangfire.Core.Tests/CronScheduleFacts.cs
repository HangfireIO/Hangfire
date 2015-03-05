using System;
using System.Collections.Generic;
using System.Linq;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class CronScheduleFacts
    {

        [Fact]
        public void Minutely_NextOccurrenceIsOneMinuteInTheFuture()
        {
            DateTime baseTime = new DateTime(2015, 02, 07, 9, 5, 32);
            string expression = Cron.Minutely();
            CrontabSchedule schedule = CrontabSchedule.Parse(expression);

            DateTime expected = new DateTime(2015, 02, 07, 9, 6, 0);
            DateTime actual = schedule.GetNextOccurrence(baseTime);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monthly_WithDayOfTheWeek_ThereIsOnlyOccuranceInNextMonthAndItIsTheFirstTuesdayOfThatMonth()
        {
            DateTime baseTime = new DateTime(2015, 2, 21, 9, 5, 32);
            DateTime expected = new DateTime(2015, 3, 3, 0, 0, 0);

            string expression = Cron.Monthly(DayOfWeek.Tuesday);
            CrontabSchedule schedule = CrontabSchedule.Parse(expression);
            DateTime actual = schedule.GetNextOccurrences(baseTime, baseTime.AddMonths(1)).Single();

            Assert.Equal(expected, actual);
        }
    }

}
