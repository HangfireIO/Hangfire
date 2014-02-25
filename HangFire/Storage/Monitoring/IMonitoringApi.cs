using System;
using System.Collections.Generic;

namespace HangFire.Storage.Monitoring
{
    public interface IMonitoringApi : IDisposable
    {
        long ScheduledCount();
        long EnqueuedCount(string queue);
        long DequeuedCount(string queue);
        long FailedCount();
        long ProcessingCount();

        IList<KeyValuePair<string, ProcessingJobDto>> ProcessingJobs(
            int from, int count);

        IList<KeyValuePair<string, ScheduleDto>> ScheduledJobs(int from, int count);
        IDictionary<DateTime, long> SucceededByDatesCount();
        IDictionary<DateTime, long> FailedByDatesCount();
        IList<ServerDto> Servers();
        IList<KeyValuePair<string, FailedJobDto>> FailedJobs(int from, int count);
        IList<KeyValuePair<string, SucceededJobDto>> SucceededJobs(int from, int count);
        IList<QueueWithTopEnqueuedJobsDto> Queues();

        IList<KeyValuePair<string, EnqueuedJobDto>> EnqueuedJobs(
            string queue, int from, int perPage);

        IList<KeyValuePair<string, DequeuedJobDto>> DequeuedJobs(
            string queue, int from, int perPage);

        IDictionary<DateTime, long> HourlySucceededJobs();
        IDictionary<DateTime, long> HourlyFailedJobs();
        bool RetryJob(string jobId);
        bool EnqueueScheduled(string jobId);
        JobDetailsDto JobDetails(string jobId);
        long SucceededListCount();
        StatisticsDto GetStatistics();
    }
}