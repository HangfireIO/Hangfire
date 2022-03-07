// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;

namespace Hangfire
{
    /// <summary>
    /// Helper class that provides common values for the cron expressions.
    /// </summary>
    public static class Cron
    {
        /// <summary>
        /// Returns cron expression that fires every minute.
        /// </summary>
        public static string Minutely()
        {
            return "* * * * *";
        }

        /// <summary>
        /// Returns cron expression that fires every hour at the first minute.
        /// </summary>
        public static string Hourly()
        {
            return Hourly(minute: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every hour at the specified minute.
        /// </summary>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static string Hourly(int minute)
        {
            return $"{minute} * * * *";
        }

        /// <summary>
        /// Returns cron expression that fires every day at 00:00 UTC.
        /// </summary>
        public static string Daily()
        {
            return Daily(hour: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every day at the first minute of
        /// the specified hour in UTC.
        /// </summary>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static string Daily(int hour)
        {
            return Daily(hour, minute: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every day at the specified hour and minute
        /// in UTC.
        /// </summary>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static string Daily(int hour, int minute)
        {
            return $"{minute} {hour} * * *";
        }

        /// <summary>
        /// Returns cron expression that fires every week at Monday, 00:00 UTC.
        /// </summary>
        public static string Weekly()
        {
            return Weekly(DayOfWeek.Monday);
        }

        /// <summary>
        /// Returns cron expression that fires every week at 00:00 UTC of the specified
        /// day of the week.
        /// </summary>
        /// <param name="dayOfWeek">The day of week in which the schedule will be activated.</param>
        public static string Weekly(DayOfWeek dayOfWeek)
        {
            return Weekly(dayOfWeek, hour: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every week at the first minute
        /// of the specified day of week and hour in UTC.
        /// </summary>
        /// <param name="dayOfWeek">The day of week in which the schedule will be activated.</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static string Weekly(DayOfWeek dayOfWeek, int hour)
        {
            return Weekly(dayOfWeek, hour, minute: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every week at the specified day
        /// of week, hour and minute in UTC.
        /// </summary>
        /// <param name="dayOfWeek">The day of week in which the schedule will be activated.</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static string Weekly(DayOfWeek dayOfWeek, int hour, int minute)
        {
            return $"{minute} {hour} * * {(int) dayOfWeek}";
        }

        /// <summary>
        /// Returns cron expression that fires every month at 00:00 UTC of the first
        /// day of month.
        /// </summary>
        public static string Monthly()
        {
            return Monthly(day: 1);
        }

        /// <summary>
        /// Returns cron expression that fires every month at 00:00 UTC of the specified
        /// day of month.
        /// </summary>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        public static string Monthly(int day)
        {
            return Monthly(day, hour: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every month at the first minute of the
        /// specified day of month and hour in UTC.
        /// </summary>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static string Monthly(int day, int hour)
        {
            return Monthly(day, hour, minute: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every month at the specified day of month,
        /// hour and minute in UTC.
        /// </summary>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static string Monthly(int day, int hour, int minute)
        {
            return $"{minute} {hour} {day} * *";
        }

        /// <summary>
        /// Returns cron expression that fires every year on Jan, 1st at 00:00 UTC.
        /// </summary>
        public static string Yearly()
        {
            return Yearly(month: 1);
        }

        /// <summary>
        /// Returns cron expression that fires every year in the first day at 00:00 UTC
        /// of the specified month.
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        public static string Yearly(int month)
        {
            return Yearly(month, day: 1);
        }

        /// <summary>
        /// Returns cron expression that fires every year at 00:00 UTC of the specified
        /// month and day of month.
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        public static string Yearly(int month, int day)
        {
            return Yearly(month, day, hour: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every year at the first minute of the
        /// specified month, day and hour in UTC.
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static string Yearly(int month, int day, int hour)
        {
            return Yearly(month, day, hour, minute: 0);
        }

        /// <summary>
        /// Returns cron expression that fires every year at the specified month, day,
        /// hour and minute in UTC.
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static string Yearly(int month, int day, int hour, int minute)
        {
            return $"{minute} {hour} {day} {month} *";
        }

		/// <summary>
		/// Returns cron expression that never fires. Specifically 31st of February
		/// </summary>
		/// <returns></returns>
		public static string Never()
		{
			return Yearly(2, 31);
		}

        /// <summary>
        /// Returns cron expression that fires every &lt;<paramref name="interval"></paramref>&gt; minutes.
        /// </summary>
        /// <param name="interval">The number of minutes to wait between every activation.</param>
        [Obsolete("Please use Cron expressions instead. Will be removed in 2.0.0")]
        public static string MinuteInterval(int interval)
        {
            return $"*/{interval} * * * *";
        }

        /// <summary>
        /// Returns cron expression that fires every &lt;<paramref name="interval"></paramref>&gt; hours.
        /// </summary>
        /// <param name="interval">The number of hours to wait between every activation.</param>
        [Obsolete("Please use Cron expressions instead. Will be removed in 2.0.0")]
        public static string HourInterval(int interval)
        {
            return $"0 */{interval} * * *";
        }

        /// <summary>
        /// Returns cron expression that fires every &lt;<paramref name="interval"></paramref>&gt; days.
        /// </summary>
        /// <param name="interval">The number of days to wait between every activation.</param>
        [Obsolete("Please use Cron expressions instead. Will be removed in 2.0.0")]
        public static string DayInterval(int interval)
        {
            return $"0 0 */{interval} * *";
        }

        /// <summary>
        /// Returns cron expression that fires every &lt;<paramref name="interval"></paramref>&gt; months.
        /// </summary>
        /// <param name="interval">The number of months to wait between every activation.</param>
        [Obsolete("Please use Cron expressions instead. Will be removed in 2.0.0")]
        public static string MonthInterval(int interval)
        {
            return $"0 0 1 */{interval} *";
        }

#if FEATURE_CRONDESCRIPTOR
        /// <summary>
        /// Converts a Cron expression string into a description.
        /// </summary>
        /// <param name="cronExpression">A Cron expression string.</param>
        /// <returns>English description.</returns>
        [Obsolete("Please install `CronExpressionDescriptor` package manually and use it.")]
        public static string GetDescription(string cronExpression)
        {
            string[] expressionParts = cronExpression.Split(' ');

            if (expressionParts.Length != 5)
            {
                throw new InvalidCastException("Invalid Cron Expression");
            }

            foreach (string expressionPart in expressionParts)
            {
                int num;
                if (!Int32.TryParse(expressionPart, out num) && expressionPart != "*")
                {
                    throw new InvalidCastException("Invalid Cron Expression");
                }
            }

            return CronExpressionDescriptor.ExpressionDescriptor.GetDescription(cronExpression);
        }
#endif
    }
}
