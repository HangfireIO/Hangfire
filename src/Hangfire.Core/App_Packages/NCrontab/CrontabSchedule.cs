#region License and Terms
//
// NCrontab - Crontab for .NET
// Copyright (c) 2008 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace NCrontab
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Debug = System.Diagnostics.Debug;

    #endregion

    /// <summary>
    /// Represents a schedule initialized from the crontab expression.
    /// </summary>

    // ReSharper disable once PartialTypeWithSinglePart

    internal sealed partial class CrontabSchedule
    {
        readonly CrontabField _seconds;
        readonly CrontabField _minutes;
        readonly CrontabField _hours;
        readonly CrontabField _days;
        readonly CrontabField _months;
        readonly CrontabField _daysOfWeek;

        static readonly CrontabField SecondZero = CrontabField.Seconds("0");

        // ReSharper disable once PartialTypeWithSinglePart

        public sealed partial class ParseOptions
        {
            public bool IncludingSeconds { get; set; }
        }

        //
        // Crontab expression format:
        //
        // * * * * *
        // - - - - -
        // | | | | |
        // | | | | +----- day of week (0 - 6) (Sunday=0)
        // | | | +------- month (1 - 12)
        // | | +--------- day of month (1 - 31)
        // | +----------- hour (0 - 23)
        // +------------- min (0 - 59)
        //
        // Star (*) in the value field above means all legal values as in
        // braces for that column. The value column can have a * or a list
        // of elements separated by commas. An element is either a number in
        // the ranges shown above or two numbers in the range separated by a
        // hyphen (meaning an inclusive range).
        //
        // Source: http://www.adminschoice.com/docs/crontab.htm
        //

        // Six-part expression format:
        //
        // * * * * * *
        // - - - - - -
        // | | | | | |
        // | | | | | +--- day of week (0 - 6) (Sunday=0)
        // | | | | +----- month (1 - 12)
        // | | | +------- day of month (1 - 31)
        // | | +--------- hour (0 - 23)
        // | +----------- min (0 - 59)
        // +------------- sec (0 - 59)
        //
        // The six-part expression behaves similarly to the traditional
        // crontab format except that it can denotate more precise schedules
        // that use a seconds component.
        //

        public static CrontabSchedule Parse(string expression) => Parse(expression, null);

        public static CrontabSchedule Parse(string expression, ParseOptions options) =>
            TryParse(expression, options, v => v, e => { throw e(); });

        public static CrontabSchedule TryParse(string expression) => TryParse(expression, null);

        public static CrontabSchedule TryParse(string expression, ParseOptions options) =>
            TryParse(expression, options, v => v, _ => null);

        public static T TryParse<T>(string expression, Func<CrontabSchedule, T> valueSelector, Func<ExceptionProvider, T> errorSelector) =>
            TryParse(expression, null, valueSelector, errorSelector);

        public static T TryParse<T>(string expression, ParseOptions options, Func<CrontabSchedule, T> valueSelector, Func<ExceptionProvider, T> errorSelector)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var tokens = expression.Split(StringSeparatorStock.Space, StringSplitOptions.RemoveEmptyEntries);

            var includingSeconds = options != null && options.IncludingSeconds;
            var expectedTokenCount = includingSeconds ? 6 : 5;
            if (tokens.Length < expectedTokenCount || tokens.Length > expectedTokenCount)
            {
                return errorSelector(() =>
                {
                    var components =
                        includingSeconds
                        ? "6 components of a schedule in the sequence of seconds, minutes, hours, days, months, and days of week"
                        : "5 components of a schedule in the sequence of minutes, hours, days, months, and days of week";
                    return new CrontabException($"'{expression}' is an invalid crontab expression. It must contain {components}.");
                });
            }

            var fields = new CrontabField[6];

            var offset = includingSeconds ? 0 : 1;
            for (var i = 0; i < tokens.Length; i++)
            {
                var kind = (CrontabFieldKind)i + offset;
                var field = CrontabField.TryParse(kind, tokens[i], v => new { ErrorProvider = (ExceptionProvider)null, Value = v },
                                                                   e => new { ErrorProvider = e, Value = (CrontabField)null });
                if (field.ErrorProvider != null)
                    return errorSelector(field.ErrorProvider);
                fields[i + offset] = field.Value;
            }

            return valueSelector(new CrontabSchedule(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5]));
        }

        CrontabSchedule(
            CrontabField seconds,
            CrontabField minutes, CrontabField hours,
            CrontabField days, CrontabField months,
            CrontabField daysOfWeek)
        {
            Debug.Assert(minutes != null);
            Debug.Assert(hours != null);
            Debug.Assert(days != null);
            Debug.Assert(months != null);
            Debug.Assert(daysOfWeek != null);

            _seconds = seconds;
            _minutes = minutes;
            _hours = hours;
            _days = days;
            _months = months;
            _daysOfWeek = daysOfWeek;
        }

        /// <summary>
        /// Enumerates all the occurrences of this schedule starting with a
        /// base time and up to an end time limit. This method uses deferred
        /// execution such that the occurrences are only calculated as they
        /// are enumerated.
        /// </summary>
        /// <remarks>
        /// This method does not return the value of <paramref name="baseTime"/>
        /// itself if it falls on the schedule. For example, if <paramref name="baseTime" />
        /// is midnight and the schedule was created from the expression <c>* * * * *</c>
        /// (meaning every minute) then the next occurrence of the schedule
        /// will be at one minute past midnight and not midnight itself.
        /// The method returns the <em>next</em> occurrence <em>after</em>
        /// <paramref name="baseTime"/>. Also, <paramref name="endTime" /> is
        /// exclusive.
        /// </remarks>

        public IEnumerable<DateTime> GetNextOccurrences(DateTime baseTime, DateTime endTime)
        {
            for (var occurrence = GetNextOccurrence(baseTime, endTime);
                 occurrence < endTime;
                 occurrence = GetNextOccurrence(occurrence, endTime))
            {
                yield return occurrence;
            }
        }

        /// <summary>
        /// Gets the next occurrence of this schedule starting with a base time.
        /// </summary>

        public DateTime GetNextOccurrence(DateTime baseTime) =>
            GetNextOccurrence(baseTime, DateTime.MaxValue);

        /// <summary>
        /// Gets the next occurrence of this schedule starting with a base
        /// time and up to an end time limit.
        /// </summary>
        /// <remarks>
        /// This method does not return the value of <paramref name="baseTime"/>
        /// itself if it falls on the schedule. For example, if <paramref name="baseTime" />
        /// is midnight and the schedule was created from the expression <c>* * * * *</c>
        /// (meaning every minute) then the next occurrence of the schedule
        /// will be at one minute past midnight and not midnight itself.
        /// The method returns the <em>next</em> occurrence <em>after</em>
        /// <paramref name="baseTime"/>. Also, <paramref name="endTime" /> is
        /// exclusive.
        /// </remarks>

        public DateTime GetNextOccurrence(DateTime baseTime, DateTime endTime)
        {
            const int nil = -1;

            var baseYear = baseTime.Year;
            var baseMonth = baseTime.Month;
            var baseDay = baseTime.Day;
            var baseHour = baseTime.Hour;
            var baseMinute = baseTime.Minute;
            var baseSecond = baseTime.Second;

            var endYear = endTime.Year;
            var endMonth = endTime.Month;
            var endDay = endTime.Day;

            var year = baseYear;
            var month = baseMonth;
            var day = baseDay;
            var hour = baseHour;
            var minute = baseMinute;
            var second = baseSecond + 1;

            //
            // Second
            //

            var seconds = _seconds ?? SecondZero;
            second = seconds.Next(second);

            if (second == nil)
            {
                second = seconds.GetFirst();
                minute++;
            }

            //
            // Minute
            //

            minute = _minutes.Next(minute);

            if (minute == nil)
            {
                minute = _minutes.GetFirst();
                hour++;
            }

            //
            // Hour
            //

            hour = _hours.Next(hour);

            if (hour == nil)
            {
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day++;
            }
            else if (hour > baseHour)
            {
                minute = _minutes.GetFirst();
            }

            //
            // Day
            //

            day = _days.Next(day);

            RetryDayMonth:

            if (day == nil)
            {
                second = seconds.GetFirst();
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day = _days.GetFirst();
                month++;
            }
            else if (day > baseDay)
            {
                second = seconds.GetFirst();
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
            }

            //
            // Month
            //

            month = _months.Next(month);

            if (month == nil)
            {
                second = seconds.GetFirst();
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day = _days.GetFirst();
                month = _months.GetFirst();
                year++;
            }
            else if (month > baseMonth)
            {
                second = seconds.GetFirst();
                minute = _minutes.GetFirst();
                hour = _hours.GetFirst();
                day = _days.GetFirst();
            }

            //
            // The day field in a cron expression spans the entire range of days
            // in a month, which is from 1 to 31. However, the number of days in
            // a month tend to be variable depending on the month (and the year
            // in case of February). So a check is needed here to see if the
            // date is a border case. If the day happens to be beyond 28
            // (meaning that we're dealing with the suspicious range of 29-31)
            // and the date part has changed then we need to determine whether
            // the day still makes sense for the given year and month. If the
            // day is beyond the last possible value, then the day/month part
            // for the schedule is re-evaluated. So an expression like "0 0
            // 15,31 * *" will yield the following sequence starting on midnight
            // of Jan 1, 2000:
            //
            //  Jan 15, Jan 31, Feb 15, Mar 15, Apr 15, Apr 31, ...
            //

            var dateChanged = day != baseDay || month != baseMonth || year != baseYear;

            if (day > 28 && dateChanged && day > Calendar.GetDaysInMonth(year, month))
            {
                if (year >= endYear && month >= endMonth && day >= endDay)
                    return endTime;

                day = nil;
                goto RetryDayMonth;
            }

            var nextTime = new DateTime(year, month, day, hour, minute, second, 0, baseTime.Kind);

            if (nextTime >= endTime)
                return endTime;

            //
            // Day of week
            //

            if (_daysOfWeek.Contains((int)nextTime.DayOfWeek))
                return nextTime;

            return GetNextOccurrence(new DateTime(year, month, day, 23, 59, 59, 0, baseTime.Kind), endTime);
        }

        /// <summary>
        /// Returns a string in crontab expression (expanded) that represents
        /// this schedule.
        /// </summary>

        public override string ToString()
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);

            if (_seconds != null)
            {
                _seconds.Format(writer, true);
                writer.Write(' ');
            }
            _minutes.Format(writer, true); writer.Write(' ');
            _hours.Format(writer, true); writer.Write(' ');
            _days.Format(writer, true); writer.Write(' ');
            _months.Format(writer, true); writer.Write(' ');
            _daysOfWeek.Format(writer, true);

            return writer.ToString();
        }

        static Calendar Calendar => CultureInfo.InvariantCulture.Calendar;
    }
}