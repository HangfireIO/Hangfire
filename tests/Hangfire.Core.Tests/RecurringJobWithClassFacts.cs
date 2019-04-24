using Hangfire.MemoryStorage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class JobWrapperNotWorking
    {
        private readonly string RecurringJobId = Guid.NewGuid().ToString();

        public Action Callback { get; set; }

        public void ScheduleAndTrigger()
        {
            RecurringJob.AddOrUpdate(RecurringJobId, () => Invoke(), () => "* * * * *");
            RecurringJob.Trigger(RecurringJobId);
            RecurringJob.RemoveIfExists(RecurringJobId);
        }

        public void Invoke()
        {
            Callback?.Invoke();
        }
    }

    public class JobWrapperWorking
    {
        private readonly string RecurringJobId = Guid.NewGuid().ToString();
        private static readonly IDictionary<string, JobWrapperWorking> s_jobId2WrapperMappings = new ConcurrentDictionary<string, JobWrapperWorking>();

        public Action Callback { get; set; }

        public void ScheduleAndTrigger()
        {
            s_jobId2WrapperMappings[RecurringJobId] = this;
            RecurringJob.AddOrUpdate(RecurringJobId, () => Invoke(RecurringJobId), () => "* * * * *");
            RecurringJob.Trigger(RecurringJobId);
            RecurringJob.RemoveIfExists(RecurringJobId);
        }

        public void Invoke(string jobId)
        {
            var wrapper = s_jobId2WrapperMappings[jobId];
            wrapper?.Callback?.Invoke();
        }
    }

    public class RecurringJobWithClassFacts
    {
        [Fact]
        public void CallbackShouldTrigger_NotWorking()
        {
            GlobalConfiguration.Configuration
                .UseMemoryStorage();

            var job = new JobWrapperNotWorking();
            bool triggered = false;
            job.Callback = () => { triggered = true; };

            job.ScheduleAndTrigger();

            Assert.Equal(false, triggered);
        }

        [Fact]
        public void CallbackShouldTrigger_Working()
        {
            GlobalConfiguration.Configuration
                .UseMemoryStorage();

            var job = new JobWrapperWorking();
            bool triggered = false;
            job.Callback = () => { triggered = true; };

            job.ScheduleAndTrigger();

            Assert.Equal(true, triggered);
        }
    }
}
