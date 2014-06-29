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
    public class DeletedState : IState
    {
        public static readonly string StateName = "Deleted";

        public DeletedState()
        {
            DeletedAt = DateTime.UtcNow;
        }

        public string Name
        {
            get { return StateName; }
        }

        public string Reason { get; set; }

        public bool IsFinal
        {
            get { return true; }
        }

        public bool IgnoreJobLoadException
        {
            get { return true; }
        }

        public DateTime DeletedAt { get; private set; }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "DeletedAt", JobHelper.SerializeDateTime(DeletedAt) }
            };
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.IncrementCounter("stats:deleted");
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.DecrementCounter("stats:deleted");
            }

            public string StateName
            {
                get { return DeletedState.StateName; }
            }
        }
    }
}
