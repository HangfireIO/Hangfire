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
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class EnqueuedState : JobState
    {
        public const string DefaultQueue = "default";

        public static readonly string Name = "Enqueued";

        public EnqueuedState(string reason) 
            : base(reason)
        {
        }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetProperties(JobDescriptor descriptor)
        {
            var queue = GetQueue(descriptor.InvocationData.Type);

            return new Dictionary<string, string>
                {
                    { "EnqueuedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "Queue", queue }
                };
        }

        public override void Apply(JobDescriptor descriptor, IRedisTransaction transaction)
        {
            var queue = GetQueue(descriptor.InvocationData.Type);

            transaction.QueueCommand(x => x.AddItemToSet("hangfire:queues", queue));
            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", queue), descriptor.JobId));
        }

        public static string GetQueue(Type jobType)
        {
            if (jobType == null) throw new ArgumentNullException("jobType");

            var attribute = jobType
                .GetCustomAttributes(true)
                .OfType<QueueAttribute>()
                .FirstOrDefault();

            var queueName = attribute != null
                ? !String.IsNullOrEmpty(attribute.Name) ? attribute.Name : DefaultQueue
                : DefaultQueue;
            ValidateQueueName(queueName);

            return queueName;
        }

        public static void ValidateQueueName(string queue)
        {
            if (String.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentNullException("queue");
            }

            if (!Regex.IsMatch(queue, @"^[a-z0-9_]+$"))
            {
                throw new InvalidOperationException(String.Format(
                    "The queue name must consist of lowercase letters, digits and underscore characters only. Given: '{0}'", queue));
            }
        }
    }
}
