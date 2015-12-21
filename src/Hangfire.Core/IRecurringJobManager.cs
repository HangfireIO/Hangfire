using Hangfire.Common;

namespace Hangfire
{
    public interface IRecurringJobManager
    {
        void AddOrUpdate(string recurringJobId, Job job, string cronExpression);

        void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options);

        void Trigger(string recurringJobId);

        void RemoveIfExists(string recurringJobId);
    }
}