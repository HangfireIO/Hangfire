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
using Hangfire.Annotations;
using NCrontab;

namespace Hangfire.Server
{
    internal class ScheduleInstant : IScheduleInstant
    {
        private readonly CrontabSchedule _schedule;

        public ScheduleInstant(DateTime utcTime, [NotNull] CrontabSchedule schedule)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Only DateTime values in UTC should be passed.", "utcTime");
            }

            if (schedule == null) throw new ArgumentNullException("schedule");

            _schedule = schedule;

            UtcTime = utcTime.AddSeconds(-utcTime.Second);
            NextOccurrence = _schedule.GetNextOccurrence(UtcTime);
        }

        public DateTime UtcTime { get; private set; }
        public DateTime NextOccurrence { get; private set; }

        public IEnumerable<DateTime> GetMatches(DateTime? lastMachingTime)
        {
            if (lastMachingTime.HasValue && lastMachingTime.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Only DateTime values in UTC should be passed.", "lastMachingTime");
            }
            
            var baseTime = lastMachingTime ?? UtcTime.AddSeconds(-1);
            var endTime = UtcTime.AddSeconds(1);

            return _schedule.GetNextOccurrences(baseTime, endTime);
        }
    }
}