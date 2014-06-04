// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using NCrontab;

namespace HangFire
{
    /// <summary>
    /// Helper class that provides common values for the
    /// <see cref="CrontabSchedule"/> class.
    /// </summary>
    public class Cron
    {
        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every minute.
        /// </summary>
        public static CrontabSchedule Minutely()
        {
            return Parse("* * * * *");
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every hour at the first minute.
        /// </summary>
        public static CrontabSchedule Hourly()
        {
            return Hourly(minute: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every hour at the specified minute.
        /// </summary>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static CrontabSchedule Hourly(int minute)
        {
            return Parse(String.Format("{0} * * * *", minute));
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every day at 00:00 UTC.
        /// </summary>
        public static CrontabSchedule Daily()
        {
            return Daily(hour: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every day at the first minute of the specified hour in UTC.
        /// </summary>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static CrontabSchedule Daily(int hour)
        {
            return Daily(hour, minute: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every day at the specified hour and minute in UTC.
        /// </summary>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static CrontabSchedule Daily(int hour, int minute)
        {
            return Parse(String.Format("{0} {1} * * *", minute, hour));
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every week at Monday, 00:00 UTC.
        /// </summary>
        public static CrontabSchedule Weekly()
        {
            return Weekly(DayOfWeek.Monday);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every week at 00:00 UTC of the specified day of week and hour.
        /// </summary>
        /// <param name="dayOfWeek">The day of week in which the schedule will be activated.</param>
        public static CrontabSchedule Weekly(DayOfWeek dayOfWeek)
        {
            return Weekly(dayOfWeek, hour: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every week at the first minute of the specified day of 
        /// week and hour (in UTC).
        /// </summary>
        /// <param name="dayOfWeek">The day of week in which the schedule will be activated.</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static CrontabSchedule Weekly(DayOfWeek dayOfWeek, int hour)
        {
            return Weekly(dayOfWeek, hour, minute: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every week at the specified day of week, hour and minute (in UTC).
        /// </summary>
        /// <param name="dayOfWeek">The day of week in which the schedule will be activated.</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static CrontabSchedule Weekly(DayOfWeek dayOfWeek, int hour, int minute)
        {
            return Parse(String.Format("{0} {1} * * {2}", minute, hour, (int) dayOfWeek));
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every month at 00:00 UTC of the first day of month.
        /// </summary>
        public static CrontabSchedule Monthly()
        {
            return Monthly(day: 1);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every month at 00:00 UTC of the specified day of month.
        /// </summary>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        public static CrontabSchedule Monthly(int day)
        {
            return Monthly(day, hour: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every month at the first minute of the specified day
        /// of month and hour (int UTC).
        /// </summary>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static CrontabSchedule Monthly(int day, int hour)
        {
            return Monthly(day, hour, minute: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every month at the specified day of month, hour and minute (in UTC).
        /// </summary>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static CrontabSchedule Monthly(int day, int hour, int minute)
        {
            return Parse(String.Format("{0} {1} {2} * *", minute, hour, day));
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every year on Jan, 1st at 00:00 UTC.
        /// </summary>
        public static CrontabSchedule Yearly()
        {
            return Yearly(month: 1);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every year in the first day at 00:00 UTC of the specified 
        /// month.
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        public static CrontabSchedule Yearly(int month)
        {
            return Yearly(month, day: 1);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every year at 00:00 of the specified month and day (in UTC).
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        public static CrontabSchedule Yearly(int month, int day)
        {
            return Yearly(month, day, hour: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every year in the first minute of the specified month, 
        /// day and hour (in UTC).
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        public static CrontabSchedule Yearly(int month, int day, int hour)
        {
            return Yearly(month, day, hour, minute: 0);
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class scheduled
        /// to be activated every year in the specified month, day, hour and minute (in UTC).
        /// </summary>
        /// <param name="month">The month in which the schedule will be activated (1-12).</param>
        /// <param name="day">The day of month in which the schedule will be activated (1-31).</param>
        /// <param name="hour">The hour in which the schedule will be activated (0-23).</param>
        /// <param name="minute">The minute in which the schedule will be activated (0-59).</param>
        public static CrontabSchedule Yearly(int month, int day, int hour, int minute)
        {
            return Parse(String.Format("{0} {1} {2} {3} *", minute, hour, day, month));
        }

        /// <summary>
        /// Returns an instance of the <see cref="CrontabSchedule"/> class initialized
        /// with the given CRON expression.
        /// </summary>
        /// <param name="expression">CRON expression (for example, "0 12 * */2").</param>
        public static CrontabSchedule Parse(string expression)
        {
            return CrontabSchedule.Parse(expression);
        }
    }
}
