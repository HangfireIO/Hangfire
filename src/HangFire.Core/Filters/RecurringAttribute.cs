// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using HangFire.Common.Filters;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Filters
{
    public class RecurringAttribute : JobFilterAttribute, IStateChangingFilter
    {
        public RecurringAttribute(int intervalInSeconds)
        {
            RepeatInterval = intervalInSeconds;
        }

        public int RepeatInterval { get; private set; }

        public void OnStateChanging(StateChangingContext context)
        {
            if (context.CandidateState.Name == SucceededState.StateName)
            {
                var scheduleAt = DateTime.UtcNow.AddSeconds(RepeatInterval);
                context.CandidateState = new ScheduledState(scheduleAt)
                {
                    Reason = "Scheduled as a recurring job"
                };
            }
        }
    }
}
