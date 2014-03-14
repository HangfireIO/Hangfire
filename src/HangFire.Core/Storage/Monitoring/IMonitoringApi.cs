using System;
using System.Collections.Generic;

namespace HangFire.Storage.Monitoring
{
    public interface IMonitoringApi : IDisposable
    {
        IList<QueueWithTopEnqueuedJobsDto> Queues();
        IList<ServerDto> Servers();
        JobDetailsDto JobDetails(string jobId);
        StatisticsDto GetStatistics();

        JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage);
        JobList<DequeuedJobDto> DequeuedJobs(string queue, int from, int perPage);

        JobList<ProcessingJobDto> ProcessingJobs(int from, int count);
        JobList<ScheduleDto> ScheduledJobs(int from, int count);
        JobList<SucceededJobDto> SucceededJobs(int from, int count);
        JobList<FailedJobDto> FailedJobs(int from, int count);

        long ScheduledCount();
        long EnqueuedCount(string queue);
        long DequeuedCount(string queue);
        long FailedCount();
        long ProcessingCount();

        long SucceededListCount();
        
        IDictionary<DateTime, long> SucceededByDatesCount();
        IDictionary<DateTime, long> FailedByDatesCount();
        IDictionary<DateTime, long> HourlySucceededJobs();
        IDictionary<DateTime, long> HourlyFailedJobs();
    }
}