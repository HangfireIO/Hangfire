using System;
using Hangfire.Common;

namespace Hangfire
{
    public interface IRecurringJobManager
    {
        void AddOrUpdate(string recurringJobId, Job job, string cronExpression);

        void AddOrUpdate(string recurringJobId, Job job, string cronExpression, TimeZoneInfo timeZone);

        void AddOrUpdate(string recurringJobId, Job job, string cronExpression, TimeZoneInfo timeZone, string queue);

        void Trigger(string recurringJobId);

        void RemoveIfExists(string recurringJobId);
    }
}