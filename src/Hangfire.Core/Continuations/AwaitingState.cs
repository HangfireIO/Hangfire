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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;

namespace Hangfire.Continuations
{
    public class AwaitingState : IState
    {
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(365);
        public static string StateName = "Awaiting";

        public AwaitingState(string parentId)
            : this(parentId, new EnqueuedState())
        {
        }

        public AwaitingState(string parentId, IState nextState)
            : this(parentId, nextState, JobContinuationOptions.OnAnyFinishedState)
        {
        }

        public AwaitingState(string parentId, IState nextState, JobContinuationOptions options)
            : this(parentId, nextState, options, DefaultExpiration)
        {
        }

        [JsonConstructor]
        public AwaitingState(
            [NotNull] string parentId,
            [NotNull] IState nextState,
            JobContinuationOptions options,
            TimeSpan expiration)
        {
            if (parentId == null) throw new ArgumentNullException("parentId");
            if (nextState == null) throw new ArgumentNullException("nextState");

            ParentId = parentId;
            NextState = nextState;

            Options = options;
            Expiration = expiration;
        }

        public string ParentId { get; private set; }
        public IState NextState { get; private set; }

        public JobContinuationOptions Options { get; private set; }
        public TimeSpan Expiration { get; private set; }

        public string Name { get { return StateName; } }
        public string Reason { get; set; }

        public bool IsFinal { get { return false; } }
        public bool IgnoreJobLoadException { get { return false; } }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "ParentId", ParentId },
                { "NextState", JsonConvert.SerializeObject(NextState, Formatting.None, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }) },
                { "Options", Options.ToString("G") },
                { "Expiration", Expiration.ToString() }
            };
        }

        public class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.AddToSet("awaiting", context.JobId, JobHelper.ToTimestamp(DateTime.UtcNow));
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.RemoveFromSet("awaiting", context.JobId);
            }

            public string StateName
            {
                get { return AwaitingState.StateName; }
            }
        }
    }
}