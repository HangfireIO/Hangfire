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
using Hangfire.Storage.Monitoring;
using Hangfire.Dashboard;

namespace Hangfire.Storage
{
    public interface IMonitoringApi
    {
        IList<QueueWithTopEnqueuedJobsDto> Queues();
        IList<ServerDto> Servers();
        JobDetailsDto JobDetails(string jobId);
        StatisticsDto GetStatistics();

        JobList<EnqueuedJobDto> EnqueuedJobs(string queue, Pager pager);
        JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage);
                
        JobList<ProcessingJobDto> ProcessingJobs(Pager pager);
        JobList<ScheduledJobDto> ScheduledJobs(Pager pager);
        JobList<SucceededJobDto> SucceededJobs(Pager pager);
        JobList<FailedJobDto> FailedJobs(Pager pager);
        JobList<DeletedJobDto> DeletedJobs(Pager pager);

        long JobCountByStateName(Dictionary<string, string> parameters);
        long EnqueuedCount(string queue, Dictionary<string,string> countParameters);
        long FetchedCount(string queue);        

        IDictionary<DateTime, long> SucceededByDatesCount();
        IDictionary<DateTime, long> FailedByDatesCount();
        IDictionary<DateTime, long> HourlySucceededJobs();
        IDictionary<DateTime, long> HourlyFailedJobs();
    }
}