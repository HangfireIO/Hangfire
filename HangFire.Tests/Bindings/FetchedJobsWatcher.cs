using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class FetchedJobsWatcher : Steps
    {
        [Given(@"a job at the fail point #1 that was checked a (.+) ago")]
        public void GivenCheckedASecondAgoJobAtTheFailPoint(string timeAgo)
        {
            Given("a fetched job");

            DateTime time;
            if (timeAgo.Equals("millisecond")) time = DateTime.UtcNow.AddMilliseconds(-1);
            else if (timeAgo.Equals("day")) time = DateTime.UtcNow.AddDays(-1);
            else throw new InvalidOperationException(String.Format("Unknown period '{0}'.", timeAgo));

            Redis.Client.SetEntry(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId),
                JobHelper.ToStringTimestamp(time));
        }

        [When(@"the watcher runs")]
        public void WhenTimedOutJobsHandlerRuns()
        {
            using (var watcher = new Server.FetchedJobsWatcher(ServerSteps.DefaultServerName))
            {
                watcher.FindAndRequeueTimedOutJobs();
            }
        }
        
        [Then(@"it marks the job as checked")]
        public void ThenItMarksTheJobAsChecked()
        {
            var checkedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId));

            Assert.IsNotNull(checkedTimestamp);
            var date = JobHelper.FromStringTimestamp(checkedTimestamp);

            Assert.IsTrue(date > DateTime.UtcNow.AddMinutes(-1));
        }

        [Then(@"the job has the checked flag set")]
        public void TheJobHasTheCheckedFlagSet()
        {
            var fetchedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:checked", JobSteps.DefaultJobId));

            Assert.IsNotNull(fetchedTimestamp);
        }

        [Then(@"the job does not have the checked flag set")]
        public void TheJobDoesNotHaveTheCheckedFlagSet()
        {
            var fetchedTimestamp = Redis.Client.GetValue(
                String.Format("hangfire:job:{0}:fetched", JobSteps.DefaultJobId));

            Assert.IsNull(fetchedTimestamp);
        }
    }
}
