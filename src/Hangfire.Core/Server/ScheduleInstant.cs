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

namespace Hangfire.Server
{
    internal class ScheduleInstant : IScheduleInstant
    {
        private readonly CrontabSchedule _schedule;

        public ScheduleInstant(DateTimeOffset now, [NotNull] CrontabSchedule schedule)
        {
            if (schedule == null) throw new ArgumentNullException("schedule");

            _schedule = schedule;

            Now = now.AddSeconds(-now.Second);
            NextOccurrence = _schedule.GetNextOccurrence(Now.UtcDateTime);
        }

        public DateTimeOffset Now { get; private set; }
        public DateTimeOffset NextOccurrence { get; private set; }

        public IEnumerable<DateTimeOffset> GetMatches(DateTimeOffset? lastMachingTime)
        {
            var baseTime = lastMachingTime ?? Now.AddSeconds(-1);
            var endTime = Now.AddSeconds(1);

            return _schedule.GetNextOccurrences(baseTime.UtcDateTime, endTime.UtcDateTime)
                .Select(x => (DateTimeOffset)x)
                .ToList();
        }
    }
}