using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

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

            Redis.Client.SetEntry(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId),
                JobHelper.ToStringTimestamp(time));
        }

        [Given(@"it was fetched a (.+) ago")]
        public void GivenItWasFetchedAAgo(string timeAgo)
        {
            DateTime time;
            if (timeAgo.Equals("millisecond")) time = DateTime.UtcNow.AddMilliseconds(-1);
            else if (timeAgo.Equals("day")) time = DateTime.UtcNow.AddDays(-1);
            else throw new InvalidOperationException(String.Format("Unknown period '{0}'.", timeAgo));

            Redis.Client.SetEntry(
                String.Format("hangfire:job:{0}:fetched", JobSteps.DefaultJobId),
                JobHelper.ToStringTimestamp(time));
        }

        [When(@"the watcher runs")]
        public void WhenTimedOutJobsHandlerRuns()
        {
            using (var watcher = new Server.DequeuedJobsWatcher(ServerSteps.DefaultServerName))
            {
                watcher.FindAndRequeueTimedOutJobs();
            }
        }
        
        [Then(@"it marks the job as 'checked'")]
        public void ThenItMarksTheJobAsChecked()
        {
            var checkedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId));

            Assert.IsNotNull(checkedTimestamp);
            var date = JobHelper.FromStringTimestamp(checkedTimestamp);

            Assert.IsTrue(date > DateTime.UtcNow.AddMinutes(-1));
        }

        [Then(@"the job has the 'checked' flag set")]
        public void ThenTheJobHasTheCheckedFlagSet()
        {
            var checkedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId));

            Assert.IsNotNull(checkedTimestamp);
        }

        [Then(@"the job does not have the 'checked' flag set")]
        public void ThenTheJobDoesNotHaveTheCheckedFlagSet()
        {
            var checkedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId));

            Assert.IsNull(checkedTimestamp);
        }

        [Then(@"the job has the 'fetched' flag set")]
        public void ThenTheJobHasTheFetchedFlagSet()
        {
            var fetchedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:fetched", JobSteps.DefaultJobId));

            Assert.IsNotNull(fetchedTimestamp);
        }

        [Then(@"the job does not have the 'fetched' flag set")]
        public void ThenTheJobDoesNotHaveTheFetchedFlagSet()
        {
            var fetchedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:fetched", JobSteps.DefaultJobId));

            Assert.IsNull(fetchedTimestamp);
        }
    }
}
