// This file is part of Hangfire.
// Copyright © 2021 Hangfire OÜ.
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
using Hangfire.Storage.Monitoring;

namespace Hangfire.Storage
{
    public abstract class JobStorageMonitor : IMonitoringApi
    {
        public abstract IList<QueueWithTopEnqueuedJobsDto> Queues();
        public abstract IList<ServerDto> Servers();
        public abstract JobDetailsDto JobDetails(string jobId);
        public abstract StatisticsDto GetStatistics();
        public abstract JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage);
        public abstract JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage);
        public abstract JobList<ProcessingJobDto> ProcessingJobs(int from, int count);
        public abstract JobList<ScheduledJobDto> ScheduledJobs(int from, int count);
        public abstract JobList<SucceededJobDto> SucceededJobs(int from, int count);
        public abstract JobList<FailedJobDto> FailedJobs(int from, int count);
        public abstract JobList<DeletedJobDto> DeletedJobs(int from, int count);
        public abstract JobList<AwaitingJobDto> AwaitingJobs(int from, int count);
        public abstract long ScheduledCount();
        public abstract long EnqueuedCount(string queue);
        public abstract long FetchedCount(string queue);
        public abstract long FailedCount();
        public abstract long ProcessingCount();
        public abstract long SucceededListCount();
        public abstract long DeletedListCount();
        public abstract long AwaitingCount();
        public abstract IDictionary<DateTime, long> SucceededByDatesCount();
        public abstract IDictionary<DateTime, long> FailedByDatesCount();
        public abstract IDictionary<DateTime, long> DeletedByDatesCount();
        public abstract IDictionary<DateTime, long> HourlySucceededJobs();
        public abstract IDictionary<DateTime, long> HourlyFailedJobs();
        public abstract IDictionary<DateTime, long> HourlyDeletedJobs();
    }
}