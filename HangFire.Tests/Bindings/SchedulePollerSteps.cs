using System;
using HangFire.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class SchedulePollerSteps : Steps
    {
        private bool _pollerResult;

        [Given(@"a scheduled job")]
        public void GivenAScheduledJob()
        {
            Given("a job");
            And("its state is Scheduled");

            Redis.Client.AddItemToSortedSet(
                "hangfire:schedule",
                JobSteps.DefaultJobId,
                JobHelper.ToTimestamp(DateTime.UtcNow.AddMinutes(-1)));
        }

        [Given(@"a scheduled job of the '(.+)' type")]
        public void GivenAScheduleJobOfType(string type)
        {
            Given(String.Format("a job of the '{0}' type", type));
            And("its state is Scheduled");

            Redis.Client.AddItemToSortedSet(
                "hangfire:schedule",
                JobSteps.DefaultJobId,
                JobHelper.ToTimestamp(DateTime.UtcNow.AddMinutes(-1)));
        }

        [Given(@"a future job")]
        public void GivenAFutureJob()
        {
            Given("a job");
            And("its state is Scheduled");

            Redis.Client.AddItemToSortedSet(
                "hangfire:schedule",
                JobSteps.DefaultJobId,
                JobHelper.ToTimestamp(DateTime.UtcNow.AddHours(1)));
        }

        [When(@"the poller runs")]
        public void WhenThePollerRuns()
        {
            using (var poller = new SchedulePoller(RedisFactory.BasicManager, TimeSpan.FromSeconds(15)))
            {
                _pollerResult = poller.EnqueueNextScheduledJob();
            }
        }

        [Then(@"the schedule should not contain it anymore")]
        public void ThenTheScheduleDoesNotContainItAnymore()
        {
            Assert.IsFalse(Redis.Client.SortedSetContainsItem(
                "hangfire:schedule",
                JobSteps.DefaultJobId));
        }

        [Then(@"the schedule should contain the job")]
        public void ThenTheScheduleContainsTheJob()
        {
            Assert.IsTrue(Redis.Client.SortedSetContainsItem(
                "hangfire:schedule",
                JobSteps.DefaultJobId));
        }

        [Then(@"schedule poller should return '(.+)'")]
        public void ThenTheSchedulePollerReturns(bool result)
        {
            Assert.AreEqual(result, _pollerResult);
        }
    }
}
