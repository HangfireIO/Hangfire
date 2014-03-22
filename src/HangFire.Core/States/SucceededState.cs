// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;

namespace HangFire.States
{
    public class SucceededState : State
    {
        public static readonly string StateName = "Succeeded";

        public SucceededState()
        {
            SucceededAt = DateTime.UtcNow;
        }

        public DateTime SucceededAt { get; set; }

        public override string Name
        {
            get { return StateName; }
        }

        public override bool ExpireJobOnApply
        {
            get { return true; }
        }

        public override Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>
            {
                { "SucceededAt",  JobHelper.ToStringTimestamp(SucceededAt)}
            };
        }

        public class Handler : StateHandler
        {
            public override void Apply(
                ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.IncrementCounter("stats:succeeded");
            }

            public override void Unapply(
                ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.DecrementCounter("stats:succeeded");
            }

            public override string StateName
            {
                get { return SucceededState.StateName; }
            }
        }
    }
}
