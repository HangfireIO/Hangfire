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
using Hangfire.Server;

namespace Hangfire
{
    public abstract class SchedulerResolution
    {
        private static SchedulerResolution _current = new MinuteSchedulerResolution();

        /// <summary>
        /// Gets or sets the current <see cref="SchedulerResolution"/> instance 
        /// that will be used to adjust when scheduler will wake.
        /// </summary>
        internal static SchedulerResolution Current
        {
            get { return _current; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _current = value;
            }
        }

        /// <summary>
        /// Get throttler for current resolution
        /// </summary>
        /// <returns>A <see cref="Throttler"/> instance for current resolution</returns>
        internal abstract IThrottler CreateThrottler();

        /// <summary>
        /// Get schedule polling interval for current resolution
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> that specifies the current schedule polling interval</returns>
        internal abstract TimeSpan GetSchedulePollingInterval();

        /// <summary>
        /// Get job initialization wait timeout for current resolution
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> that specifies the current job initialization wait timeout</returns>
        internal abstract TimeSpan GetJobInitializationWaitTimeout();

        /// <summary>
        /// Caculates the base time tjhat is required to calculate next instant
        /// </summary>
        /// <param name="nowInstant">Current base</param>
        /// <param name="timeZone">Time zone</param>
        /// <param name="getNextOccurrence">This argument is the calculation function that returns next occurance time based on given time</param>
        /// <returns>A <see cref="DateTime"/> that can be used as base time for <see cref="ScheduleInstant"/> classes</returns>
        internal abstract DateTime CalculateNowInstant(DateTime nowInstant, TimeZoneInfo timeZone, Func<DateTime, DateTime> getNextOccurrence);
    }
}