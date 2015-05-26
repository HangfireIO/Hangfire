// This file is part of Hangfire.
// Copyright � 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Annotations;
using NCrontab;

namespace Hangfire.Server
{
    internal class ScheduleInstant : IScheduleInstant
    {
        private readonly TimeZoneInfo _timeZone;
        private readonly CrontabSchedule _schedule;

        public ScheduleInstant(DateTime nowInstant, TimeZoneInfo timeZone, [NotNull] CrontabSchedule schedule)
        {
            if (schedule == null) throw new ArgumentNullException("schedule");
            if (nowInstant.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Only DateTime values in UTC should be passed.", "nowInstant");
            }

            _timeZone = timeZone;
            _schedule = schedule;

            NowInstant = nowInstant.AddSeconds(-nowInstant.Second);
            NextInstant = TimeZoneInfo.ConvertTimeToUtc(
                _schedule.GetNextOccurrence(TimeZoneInfo.ConvertTimeFromUtc(NowInstant, _timeZone)),
                _timeZone);
        }

        public DateTime NowInstant { get; private set; }
        public DateTime NextInstant { get; private set; }

        public IEnumerable<DateTime> GetNextInstants(DateTime? lastInstant)
        {
            if (lastInstant.HasValue && lastInstant.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Only DateTime values in UTC should be passed.", "lastInstant");
            }

            var baseTime = lastInstant ?? NowInstant.AddSeconds(-1);
            var endTime = NowInstant.AddSeconds(1);

            return _schedule
                .GetNextOccurrences(
                    TimeZoneInfo.ConvertTimeFromUtc(baseTime, _timeZone),
                    TimeZoneInfo.ConvertTimeFromUtc(endTime, _timeZone))
                .Select(x => TimeZoneInfo.ConvertTimeToUtc(x, _timeZone))
                .ToList();
        }
    }
}