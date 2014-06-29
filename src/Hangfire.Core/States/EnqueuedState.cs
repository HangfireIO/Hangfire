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
using System.Text.RegularExpressions;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class EnqueuedState : IState
    {
        public const string DefaultQueue = "default";
        public static readonly string StateName = "Enqueued";

        private string _queue;

        public EnqueuedState()
            : this(DefaultQueue)
        {
        }

        public EnqueuedState(string queue)
        {
            EnqueuedAt = DateTime.UtcNow;
            Queue = queue;
        }

        public string Queue
        {
            get { return _queue; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!Regex.IsMatch(value, @"^[a-z0-9_]+$"))
                {
                    throw new ArgumentException(
                        String.Format(
                            "The queue name must consist of lowercase letters, digits and underscore characters only. Given: '{0}'.", value),
                        "value");
                }

                _queue = value;
            }
        }

        public DateTime EnqueuedAt { get; set; }

        public string Name { get { return StateName; } }
        public string Reason { get; set; }
        public bool IsFinal { get { return false; } }
        public bool IgnoreJobLoadException { get { return false; } }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "EnqueuedAt", JobHelper.SerializeDateTime(EnqueuedAt) },
                { "Queue", Queue }
            };
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                var enqueuedState = context.NewState as EnqueuedState;
                if (enqueuedState == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "`{0}` state handler can be registered only for the Enqueued state.",
                        typeof(Handler).FullName));
                }

                transaction.AddToQueue(enqueuedState.Queue, context.JobId);
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
            }

            public string StateName
            {
                get { return EnqueuedState.StateName; }
            }
        }
    }
}
