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
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;

namespace HangFire.States
{
    public class ScheduledState : State
    {
        public static readonly string StateName = "Scheduled";
        
        public ScheduledState(DateTime enqueueAt)
        {
            EnqueueAt = enqueueAt;
            ScheduledAt = DateTime.UtcNow;
        }

        public DateTime EnqueueAt { get; set; }
        public DateTime ScheduledAt { get; set; }

        public override string Name { get { return StateName; } }

        public override Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>
            {
                { "EnqueueAt", JobHelper.ToStringTimestamp(EnqueueAt) },
                { "ScheduledAt", JobHelper.ToStringTimestamp(ScheduledAt) }
            };
        }

        public class Handler : StateHandler
        {
            public override void Apply(
                StateApplyingContext context, IWriteOnlyTransaction transaction)
            {
                var scheduledState = context.NewState as ScheduledState;
                if (scheduledState == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "`{0}` state handler can be registered only for the Scheduled state.",
                        typeof(Handler).FullName));
                }

                var timestamp = JobHelper.ToTimestamp(scheduledState.EnqueueAt);
                transaction.AddToSet("schedule", context.JobId, timestamp);
            }

            public override void Unapply(
                StateApplyingContext context, IWriteOnlyTransaction transaction)
            {
                transaction.RemoveFromSet("schedule", context.JobId);
            }

            public override string StateName
            {
                get { return ScheduledState.StateName; }
            }
        }
    }
}
