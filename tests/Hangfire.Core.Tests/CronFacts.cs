using System;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class CronFacts
    {
        [Fact]
        public void Minutely_ReturnsFormattedString()
        {
            string expected = "* * * * *";
            string actual = Cron.Minutely();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Hourly_WithoutMinute_ReturnsFormattedStringWithDefaults()
        {
            string expected = "0 * * * *";
            string actual = Cron.Hourly();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Hourly_WithMinute_ReturnsFormattedStringWithMinute()
        {
            string expected = "5 * * * *";
            string actual = Cron.Hourly(5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Daily_WithoutMinuteOrHour_ReturnsFormattedStringWithDefaults()
        {
            string expected = "0 0 * * *";
            string actual = Cron.Daily();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Daily_WithoutMinute_ReturnsFormattedStringWithHourAndZeroMinute()
        {
            string expected = "0 5 * * *";
            string actual = Cron.Daily(5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Daily_WithMinuteAndHour_ReturnsFormattedStringWithHourAndMinute()
        {
            string expected = "5 5 * * *";
            string actual = Cron.Daily(5, 5);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Weekly_WithoutDayHourMinute_ReturnsFormattedStringWithDefaults()
        {
            string expected = "0 0 * * " + ((int)DayOfWeek.Monday).ToString();
            string actual = Cron.Weekly();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Weekly_WithDayWithoutHourMinute_ReturnsFormattedStringWithDay()
        {
            DayOfWeek day = DayOfWeek.Thursday;
            string expected = "0 0 * * " + ((int)day).ToString();
            string actual = Cron.Weekly(day);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Weekly_WithDayHourWithoutMinute_ReturnsFormattedStringWithDayHour()
        {
            DayOfWeek day = DayOfWeek.Thursday;
            int hour = 5;
            string expected = "0 " + hour.ToString() + " * * " + ((int)day).ToString();
            string actual = Cron.Weekly(day, hour);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Weekly_WithDayHourMinute_ReturnsFormattedStringWithDayHourMinute()
        {
            DayOfWeek day = DayOfWeek.Thursday;
            int hour = 5;
            int minute = 5;
            string expected = minute.ToString() + " " + hour.ToString() + " * * " + ((int)day).ToString();
            string actual = Cron.Weekly(day, hour, minute);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monthly_WithoutDayHourMinute_ReturnsFormattedStringWithDefaults()
        {
            string expected = "0 0 1 * *";
            string actual = Cron.Monthly();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monthly_WithoutHourMinuteWithDay_ReturnsFormattedStringWithDay()
        {
            int day = 6;
            string expected = "0 0 " + day.ToString() + " * *";
            string actual = Cron.Monthly(day);
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public void Monthly_WithoutMinuteWithDayHour_ReturnsFormattedStringWithDayHour()
        {
            int day = 7;
            int hour = 4;
            string expected = "0 " + hour.ToString() + " " + day.ToString() + " * *";
            string actual = Cron.Monthly(day, hour);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Monthly_WithDayHourMinute_ReturnsFormattedStringWithDayHourMinute()
        {
            int day = 7;
            int hour = 4;
            int minute = 23;
            string expected = minute.ToString() + " " + hour.ToString() + " " + day.ToString() + " * *";
            string actual = Cron.Monthly(day, hour, minute);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Yearly_WithoutMonthDayHourMinute_ReturnsFormattedStringWithDefaults()
        {
            string expected = "0 0 1 1 *";
            string actual = Cron.Yearly();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Yearly_WithoutDayHourMinuteWithMonth_ReturnsFormattedStringWithMonth()
        {
            int month = 7;
            string expected = "0 0 1 " + month.ToString() + " *";
            string actual = Cron.Yearly(month);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Yearly_WithoutHourMinuteWithMonthDay_ReturnsFormattedStringWithMonthDay()
        {
            int month = 8;
            int day = 18;
            string expected = "0 0 " + day.ToString() + " " + month.ToString() + " *";
            string actual = Cron.Yearly(month, day);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Yearly_WithoutMinuteWithMonthDayHour_ReturnsFormattedStringWithMonthDayHour()
        {
            int month = 3;
            int day = 18;
            int hour = 14;
            string expected = "0 " + hour.ToString() + " " + day.ToString() + " " + month.ToString() + " *";
            string actual = Cron.Yearly(month, day, hour);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Yearly_WithMonthDayHourMinute_ReturnsFormattedStringWithMonthDayHourMinute()
        {
            int month = 4;
            int day = 17;
            int hour = 3;
            int minute = 45;
            string expected = minute.ToString() + " " + hour.ToString() + " " + day.ToString() + " " + month.ToString() + " *";
            string actual = Cron.Yearly(month, day, hour, minute);
            Assert.Equal(expected, actual);
        }

		[Fact]
		public void Never_ReturnsFormattedString()
		{
			string expected = "0 0 31 2 *";
			string actual = Cron.Yearly(2, 31);
			Assert.Equal(expected, actual);
		}
    }
}
