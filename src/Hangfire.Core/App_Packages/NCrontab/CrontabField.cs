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
    using System.Collections;
    using System.Globalization;
    using System.IO;

    #endregion

    /// <summary>
    /// Represents a single crontab field.
    /// </summary>

    // ReSharper disable once PartialTypeWithSinglePart

    internal sealed partial class CrontabField : ICrontabField
    {
        readonly BitArray _bits;
        /* readonly */
        int _minValueSet;
        /* readonly */
        int _maxValueSet;
        readonly CrontabFieldImpl _impl;

        /// <summary>
        /// Parses a crontab field expression given its kind.
        /// </summary>

        public static CrontabField Parse(CrontabFieldKind kind, string expression) =>
            TryParse(kind, expression, v => v, e => { throw e(); });

        public static CrontabField TryParse(CrontabFieldKind kind, string expression) =>
            TryParse(kind, expression, v => v, _ => null);

        public static T TryParse<T>(CrontabFieldKind kind, string expression, Func<CrontabField, T> valueSelector, Func<ExceptionProvider, T> errorSelector)
        {
            var field = new CrontabField(CrontabFieldImpl.FromKind(kind));
            var error = field._impl.TryParse(expression, field.Accumulate, null, e => e);
            return error == null ? valueSelector(field) : errorSelector(error);
        }

        /// <summary>
        /// Parses a crontab field expression representing seconds.
        /// </summary>

        public static CrontabField Seconds(string expression) =>
            Parse(CrontabFieldKind.Second, expression);

        /// <summary>
        /// Parses a crontab field expression representing minutes.
        /// </summary>

        public static CrontabField Minutes(string expression) =>
            Parse(CrontabFieldKind.Minute, expression);

        /// <summary>
        /// Parses a crontab field expression representing hours.
        /// </summary>

        public static CrontabField Hours(string expression) =>
            Parse(CrontabFieldKind.Hour, expression);

        /// <summary>
        /// Parses a crontab field expression representing days in any given month.
        /// </summary>

        public static CrontabField Days(string expression) =>
            Parse(CrontabFieldKind.Day, expression);

        /// <summary>
        /// Parses a crontab field expression representing months.
        /// </summary>

        public static CrontabField Months(string expression) =>
            Parse(CrontabFieldKind.Month, expression);

        /// <summary>
        /// Parses a crontab field expression representing days of a week.
        /// </summary>

        public static CrontabField DaysOfWeek(string expression) =>
            Parse(CrontabFieldKind.DayOfWeek, expression);

        CrontabField(CrontabFieldImpl impl)
        {
            if (impl == null) throw new ArgumentNullException(nameof(impl));

            _impl = impl;
            _bits = new BitArray(impl.ValueCount);

            _bits.SetAll(false);
            _minValueSet = int.MaxValue;
            _maxValueSet = -1;
        }

        /// <summary>
        /// Gets the first value of the field or -1.
        /// </summary>

        public int GetFirst() => _minValueSet < int.MaxValue ? _minValueSet : -1;

        /// <summary>
        /// Gets the next value of the field that occurs after the given
        /// start value or -1 if there is no next value available.
        /// </summary>

        public int Next(int start)
        {
            if (start < _minValueSet)
                return _minValueSet;

            var startIndex = ValueToIndex(start);
            var lastIndex = ValueToIndex(_maxValueSet);

            for (var i = startIndex; i <= lastIndex; i++)
            {
                if (_bits[i])
                    return IndexToValue(i);
            }

            return -1;
        }

        int IndexToValue(int index) => index + _impl.MinValue;
        int ValueToIndex(int value) => value - _impl.MinValue;

        /// <summary>
        /// Determines if the given value occurs in the field.
        /// </summary>

        public bool Contains(int value) => _bits[ValueToIndex(value)];

        /// <summary>
        /// Accumulates the given range (start to end) and interval of values
        /// into the current set of the field.
        /// </summary>
        /// <remarks>
        /// To set the entire range of values representable by the field,
        /// set <paramref name="start" /> and <paramref name="end" /> to -1 and
        /// <paramref name="interval" /> to 1.
        /// </remarks>

        T Accumulate<T>(int start, int end, int interval, T success, Func<ExceptionProvider, T> errorSelector)
        {
            var minValue = _impl.MinValue;
            var maxValue = _impl.MaxValue;

            if (start == end)
            {
                if (start < 0)
                {
                    //
                    // We're setting the entire range of values.
                    //

                    if (interval <= 1)
                    {
                        _minValueSet = minValue;
                        _maxValueSet = maxValue;
                        _bits.SetAll(true);
                        return success;
                    }

                    start = minValue;
                    end = maxValue;
                }
                else
                {
                    //
                    // We're only setting a single value - check that it is in range.
                    //

                    if (start < minValue)
                        return OnValueBelowMinError(start, errorSelector);

                    if (start > maxValue)
                        return OnValueAboveMaxError(start, errorSelector);
                }
            }
            else
            {
                //
                // For ranges, if the start is bigger than the end value then
                // swap them over.
                //

                if (start > end)
                {
                    end ^= start;
                    start ^= end;
                    end ^= start;
                }

                if (start < 0)
                    start = minValue;
                else if (start < minValue)
                    return OnValueBelowMinError(start, errorSelector);

                if (end < 0)
                    end = maxValue;
                else if (end > maxValue)
                    return OnValueAboveMaxError(end, errorSelector);
            }

            if (interval < 1)
                interval = 1;

            int i;

            //
            // Populate the _bits table by setting all the bits corresponding to
            // the valid field values.
            //

            for (i = start - minValue; i <= (end - minValue); i += interval)
                _bits[i] = true;

            //
            // Make sure we remember the minimum value set so far Keep track of
            // the highest and lowest values that have been added to this field
            // so far.
            //

            if (_minValueSet > start)
                _minValueSet = start;

            i += (minValue - interval);

            if (_maxValueSet < i)
                _maxValueSet = i;

            return success;
        }

        T OnValueAboveMaxError<T>(int value, Func<ExceptionProvider, T> errorSelector) =>
            errorSelector(
                () => new CrontabException(
                    $"{value} is higher than the maximum allowable value for the [{_impl.Kind}] field. " +
                    $"Value must be between {_impl.MinValue} and {_impl.MaxValue} (all inclusive)."));

        T OnValueBelowMinError<T>(int value, Func<ExceptionProvider, T> errorSelector) =>
            errorSelector(
                () => new CrontabException(
                    $"{value} is lower than the minimum allowable value for the [{_impl.Kind}] field. " +
                    $"Value must be between {_impl.MinValue} and {_impl.MaxValue} (all inclusive)."));

        public override string ToString() => ToString(null);

        public string ToString(string format)
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);

            switch (format)
            {
                case "G":
                case null:
                    Format(writer, true);
                    break;
                case "N":
                    Format(writer);
                    break;
                default:
                    throw new FormatException();
            }

            return writer.ToString();
        }

        public void Format(TextWriter writer) => Format(writer, false);

        public void Format(TextWriter writer, bool noNames) =>
            _impl.Format(this, writer, noNames);
    }
}