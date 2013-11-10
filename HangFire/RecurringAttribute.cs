// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class RecurringAttribute : JobFilterAttribute, IStateChangingFilter
    {
        public RecurringAttribute(int intervalInSeconds)
        {
            RepeatInterval = intervalInSeconds;
        }

        public int RepeatInterval { get; private set; }

        public JobState OnStateChanging(
            JobDescriptor descriptor, JobState state, IRedisClient redis)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            if (state.StateName != SucceededState.Name)
            {
                return state;
            }

            return new ScheduledState(
                "Scheduled as a recurring job",
                DateTime.UtcNow.AddSeconds(RepeatInterval));
        }
    }
}
