using System;
using HangFire.Common;
using HangFire.Redis.Components;
using HangFire.Server;
using TechTalk.SpecFlow;
using Xunit;

namespace HangFire.Tests
{
    [Binding]
    public class DequeuedJobsWatcherSteps : Steps
    {
        [Given(@"it was checked a (.+) ago")]
        public void ItWasCheckedAAgo(string timeAgo)
        {
            DateTime time;
            if (timeAgo.Equals("millisecond")) time = DateTime.UtcNow.AddMilliseconds(-1);
            else if (timeAgo.Equals("day")) time = DateTime.UtcNow.AddDays(-1);
            else throw new InvalidOperationException(String.Format("Unknown period '{0}'.", timeAgo));

            Redis.Client.SetEntryInHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                "Checked",
                JobHelper.ToStringTimestamp(time));
        }

        [Given(@"it was fetched a (.+) ago")]
        public void GivenItWasFetchedAAgo(string timeAgo)
        {
            DateTime time;
            if (timeAgo.Equals("millisecond")) time = DateTime.UtcNow.AddMilliseconds(-1);
            else if (timeAgo.Equals("day")) time = DateTime.UtcNow.AddDays(-1);
            else throw new InvalidOperationException(String.Format("Unknown period '{0}'.", timeAgo));

            Redis.Client.SetEntryInHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                "Fetched",
                JobHelper.ToStringTimestamp(time));
        }

        [When(@"the watcher runs")]
        public void WhenTimedOutJobsHandlerRuns()
        {
            var watcher = new FetchedJobsWatcher(Redis.Storage);
            watcher.FindAndRequeueTimedOutJobs();
        }
        
        [Then(@"it should mark the job as 'checked'")]
        public void ThenItMarksTheJobAsChecked()
        {
            var checkedTimestamp = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                "Checked");

            Assert.NotNull(checkedTimestamp);
            var date = JobHelper.FromStringTimestamp(checkedTimestamp);

            Assert.True(date > DateTime.UtcNow.AddMinutes(-1));
        }

        [Then(@"the job should have the 'checked' flag set")]
        public void ThenTheJobHasTheCheckedFlagSet()
        {
            var checkedTimestamp = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                "Checked");

            Assert.NotNull(checkedTimestamp);
        }

        [Then(@"the job should not have the 'checked' flag set")]
        public void ThenTheJobDoesNotHaveTheCheckedFlagSet()
        {
            var checkedTimestamp = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                "Checked");

            Assert.Null(checkedTimestamp);
        }

        [Then(@"the job should have the 'fetched' flag set")]
        public void ThenTheJobHasTheFetchedFlagSet()
        {
            var fetchedTimestamp = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId), "Fetched");

            Assert.NotNull(fetchedTimestamp);
        }

        [Then(@"the job should not have the 'fetched' flag set")]
        public void ThenTheJobDoesNotHaveTheFetchedFlagSet()
        {
            var fetchedTimestamp = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", JobSteps.DefaultJobId), "Fetched");

            Assert.Null(fetchedTimestamp);
        }
    }
}
