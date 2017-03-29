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
using Hangfire.Annotations;
using Cronos;

namespace Hangfire.Server
{
    internal class ScheduleInstant : IScheduleInstant
    {
        private readonly TimeZoneInfo _timeZone;
        private readonly CronExpression _cronExpression;

        public static Func<CronExpression, TimeZoneInfo, IScheduleInstant> Factory =
            (schedule, timeZone) => new ScheduleInstant(DateTime.UtcNow, timeZone, schedule);

        public ScheduleInstant(DateTime nowInstant, TimeZoneInfo timeZone, [NotNull] CronExpression cronExpression)
        {
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (nowInstant.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Only DateTime values in UTC should be passed.", nameof(nowInstant));
            }

            _timeZone = timeZone;
            _cronExpression = cronExpression;

            NowInstant = nowInstant.AddSeconds(-nowInstant.Second);

            NextInstant = _cronExpression.GetNextOccurrence(NowInstant, _timeZone);
        }

        public DateTime NowInstant { get; }
        public DateTime? NextInstant { get; }

        public bool ShouldSchedule(DateTime lastInstant)
        {
            if (lastInstant.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Only DateTime values in UTC should be passed.", nameof(lastInstant));
            }
            var nextOccurrence = _cronExpression.GetNextOccurrence(lastInstant, _timeZone);
            
            return nextOccurrence <= NowInstant;
        }
    }
}