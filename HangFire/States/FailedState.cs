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
using System.Collections.Generic;
using HangFire.Client;

namespace HangFire.States
{
    public class FailedState : JobState
    {
        public static readonly string Name = "Failed";

        public FailedState(string reason, Exception exception) 
            : base(reason)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties(JobMethod data)
        {
            return new Dictionary<string, string>
                {
                    { "FailedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "ExceptionType", Exception.GetType().FullName },
                    { "ExceptionMessage", Exception.Message },
                    { "ExceptionDetails", Exception.ToString() }
                };
        }

        public override void Apply(StateApplyingContext context)
        {
            context.Transaction.QueueCommand(x => x.AddItemToSortedSet(
                "hangfire:failed",
                context.JobId,
                JobHelper.ToTimestamp(DateTime.UtcNow)));
        }

        public class Descriptor : JobStateDescriptor
        {
            public override void Unapply(StateApplyingContext context)
            {
                context.Transaction.QueueCommand(x => x.RemoveItemFromSortedSet(
                    "hangfire:failed", context.JobId));
            }
        }
    }
}
