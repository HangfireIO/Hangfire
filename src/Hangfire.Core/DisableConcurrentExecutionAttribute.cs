// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Globalization;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Newtonsoft.Json;

namespace Hangfire
{
    public class DisableConcurrentExecutionAttribute : JobFilterAttribute, IServerFilter
    {
        public DisableConcurrentExecutionAttribute(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0) throw new ArgumentException("Timeout argument value should be greater that zero.");

            TimeoutSec = timeoutInSeconds;
        }
        
        [JsonConstructor]
        public DisableConcurrentExecutionAttribute(string resource, int timeoutSec)
            : this(timeoutSec)
        {
            Resource = resource;
        }

        [CanBeNull]
        public string Resource { get; }
        public int TimeoutSec { get; }

        public void OnPerforming(PerformingContext context)
        {
            var resource = GetResource(context.BackgroundJob.Job);
            var timeout = TimeSpan.FromSeconds(TimeoutSec);

            var distributedLock = context.Connection.AcquireDistributedLock(resource, timeout);
            context.Items["DistributedLock"] = distributedLock;
        }

        public void OnPerformed(PerformedContext context)
        {
            if (!context.Items.ContainsKey("DistributedLock"))
            {
                throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");
            }

            var distributedLock = (IDisposable)context.Items["DistributedLock"];
            distributedLock.Dispose();
        }

        private string GetResource(Job job)
        {
            if (!String.IsNullOrWhiteSpace(Resource))
            {
                try
                {
                    return String.Format(CultureInfo.InvariantCulture, Resource, job.Args.ToArray()).ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    throw new FormatException($"Unable to obtain resource identifier: {ex.Message}");
                }
            }

            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}
