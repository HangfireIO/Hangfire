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
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Storage
{
    public interface IMonitoringApi
    {
        [NotNull] IList<QueueWithTopEnqueuedJobsDto> Queues();
        [NotNull] IList<ServerDto> Servers();

        [CanBeNull]
        JobDetailsDto JobDetails([NotNull] string jobId);

        [NotNull]
        StatisticsDto GetStatistics();

        [NotNull] JobList<EnqueuedJobDto> EnqueuedJobs([NotNull] string queue, int from, int perPage);
        [NotNull] JobList<FetchedJobDto> FetchedJobs([NotNull] string queue, int from, int perPage);

        [NotNull] JobList<ProcessingJobDto> ProcessingJobs(int from, int count);
        [NotNull] JobList<ScheduledJobDto> ScheduledJobs(int from, int count);
        [NotNull] JobList<SucceededJobDto> SucceededJobs(int from, int count);
        [NotNull] JobList<FailedJobDto> FailedJobs(int from, int count);
        [NotNull] JobList<DeletedJobDto> DeletedJobs(int from, int count);

        long ScheduledCount();
        long EnqueuedCount([NotNull] string queue);
        long FetchedCount([NotNull] string queue);
        long FailedCount();
        long ProcessingCount();

        long SucceededListCount();
        long DeletedListCount();

        [NotNull] IDictionary<DateTime, long> SucceededByDatesCount();
        [NotNull] IDictionary<DateTime, long> FailedByDatesCount();
        [NotNull] IDictionary<DateTime, long> HourlySucceededJobs();
        [NotNull] IDictionary<DateTime, long> HourlyFailedJobs();
    }
}