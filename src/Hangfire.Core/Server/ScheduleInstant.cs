// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using NodaTime;

namespace Hangfire.Server
{
    internal class ScheduleInstant : IScheduleInstant
    {
        private readonly DateTimeZone _timeZone;
        private readonly CrontabSchedule _schedule;

        public ScheduleInstant(Instant now, DateTimeZone timeZone, [NotNull] CrontabSchedule schedule)
        {
            if (schedule == null) throw new ArgumentNullException("schedule");

            _timeZone = timeZone;
            _schedule = schedule;

            var zonedNow = now.InZone(_timeZone);
            var roundedNow = zonedNow.Minus(Duration.FromSeconds(zonedNow.Second));

            Now = roundedNow.ToInstant();
            NextOccurrence = _schedule.GetNextOccurrence(roundedNow.LocalDateTime).InZoneLeniently(_timeZone).ToInstant();
        }

        public Instant Now { get; private set; }
        public Instant NextOccurrence { get; private set; }

        public IEnumerable<Instant> GetMatches(Instant? lastMachingTime)
        {
            var baseTime = lastMachingTime ?? Now.Minus(Duration.FromSeconds(-1));
            var endTime = Now.Plus(Duration.FromSeconds(1));

            return _schedule.GetNextOccurrences(baseTime.InZone(_timeZone).LocalDateTime, endTime.InZone(_timeZone).LocalDateTime)
                .Select(x => x.InZoneLeniently(_timeZone).ToInstant())
                .ToList();
        }
    }

    public static class CronScheduleExtensions
    {
        public static LocalDateTime GetNextOccurrence(this CrontabSchedule schedule, LocalDateTime baseTime)
        {
            return LocalDateTime.FromDateTime(schedule.GetNextOccurrence(baseTime.ToDateTimeUnspecified()));
        }

        public static IEnumerable<LocalDateTime> GetNextOccurrences(
            this CrontabSchedule schedule, LocalDateTime baseTime, LocalDateTime endTime)
        {
            return schedule.GetNextOccurrences(baseTime.ToDateTimeUnspecified(), endTime.ToDateTimeUnspecified())
                .Select(LocalDateTime.FromDateTime)
                .ToList();
        }
    }
}