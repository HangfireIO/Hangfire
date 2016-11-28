// This file is part of Hangfire.
// Copyright © 2016 Sergey Odinokov.
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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    public static class RecurringJobManagerExtensions
    {
        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression)
        {
            AddOrUpdate(manager, recurringJobId, job, cronExpression, TimeZoneInfo.Utc);
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression,
            [NotNull] TimeZoneInfo timeZone)
        {
            AddOrUpdate(manager, recurringJobId, job, cronExpression, timeZone, EnqueuedState.DefaultQueue);
        }

        public static void AddOrUpdate(
            [NotNull] this IRecurringJobManager manager,
            [NotNull] string recurringJobId,
            [NotNull] Job job,
            [NotNull] string cronExpression,
            [NotNull] TimeZoneInfo timeZone,
            [NotNull] string queue)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            manager.AddOrUpdate(
                recurringJobId,
                job,
                cronExpression,
                new RecurringJobOptions { QueueName = queue, TimeZone = timeZone });
        }
    }
}