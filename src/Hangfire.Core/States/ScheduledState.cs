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
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class ScheduledState : IState
    {
        public static readonly string StateName = "Scheduled";

        public ScheduledState(TimeSpan enqueueIn)
            : this(DateTime.UtcNow.Add(enqueueIn))
        {
        }
        
        public ScheduledState(DateTime enqueueAt)
        {
            EnqueueAt = enqueueAt;
            ScheduledAt = DateTime.UtcNow;
        }

        public DateTime EnqueueAt { get; set; }
        public DateTime ScheduledAt { get; set; }

        public string Name { get { return StateName; } }
        public string Reason { get; set; }
        public bool IsFinal { get { return false; } }
        public bool IgnoreJobLoadException { get { return false; } }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "EnqueueAt", JobHelper.SerializeDateTime(EnqueueAt) },
                { "ScheduledAt", JobHelper.SerializeDateTime(ScheduledAt) }
            };
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
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

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.RemoveFromSet("schedule", context.JobId);
            }

            public string StateName
            {
                get { return ScheduledState.StateName; }
            }
        }
    }
}
