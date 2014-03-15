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

namespace HangFire.States
{
    public class SucceededState : JobState
    {
        private static readonly TimeSpan JobExpirationTimeout = TimeSpan.FromDays(1);
        public static readonly string Name = "Succeeded";

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties(JobMethod data)
        {
            return new Dictionary<string, string>
                {
                    { "SucceededAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                };
        }

        public class Handler : JobStateHandler
        {
            public override void Apply(StateApplyingContext context)
            {
                context.Transaction.ExpireJob(context.JobId, JobExpirationTimeout);
                context.Transaction.IncrementCounter("stats:succeeded");
            }

            public override void Unapply(StateApplyingContext context)
            {
                context.Transaction.DecrementCounter("stats:succeeded");
                context.Transaction.PersistJob(context.JobId);
            }

            public override string StateName
            {
                get { return Name; }
            }
        }
    }
}
