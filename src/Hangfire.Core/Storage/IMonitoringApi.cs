// This file is part of Hangfire.
// Copyright � 2013-2014 Sergey Odinokov.
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
    public interface IMonitoringApi
    {
        IList<QueueWithTopEnqueuedJobsDto> Queues();
        IList<ServerDto> Servers();
        JobDetailsDto JobDetails(string jobId);
        StatisticsDto GetStatistics();

        JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage, string filter);
        JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage, string filter);

        JobList<ProcessingJobDto> ProcessingJobs(int from, int count, string filter);
        JobList<ScheduledJobDto> ScheduledJobs(int from, int count, string filter);
        JobList<SucceededJobDto> SucceededJobs(int from, int count, string filter);
        JobList<FailedJobDto> FailedJobs(int from, int count, string filter);
        JobList<DeletedJobDto> DeletedJobs(int from, int count, string filter);

        long ScheduledCount(string filter);
        long EnqueuedCount(string queue, string filter);
        long FetchedCount(string queue, string filter);
        long FailedCount(string filter);
        long ProcessingCount(string filter);

        long SucceededListCount(string filter);
        long DeletedListCount(string filter);
        
        IDictionary<DateTime, long> SucceededByDatesCount();
        IDictionary<DateTime, long> FailedByDatesCount();
        IDictionary<DateTime, long> HourlySucceededJobs();
        IDictionary<DateTime, long> HourlyFailedJobs();
    }
}