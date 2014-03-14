using System;
using HangFire.Common;
using HangFire.Redis;
using HangFire.Redis.Components;
using HangFire.Server;
using HangFire.Server.Components;
using TechTalk.SpecFlow;
using Xunit;

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
            var poller = new SchedulePoller(Redis.Storage, TimeSpan.FromSeconds(15));
            _pollerResult = poller.EnqueueNextScheduledJob();
        }

        [Then(@"the schedule should not contain it anymore")]
        public void ThenTheScheduleDoesNotContainItAnymore()
        {
            Assert.False(Redis.Client.SortedSetContainsItem(
                "hangfire:schedule",
                JobSteps.DefaultJobId));
        }

        [Then(@"the schedule should contain the job")]
        public void ThenTheScheduleContainsTheJob()
        {
            Assert.True(Redis.Client.SortedSetContainsItem(
                "hangfire:schedule",
                JobSteps.DefaultJobId));
        }

        [Then(@"schedule poller should return '(.+)'")]
        public void ThenTheSchedulePollerReturns(bool result)
        {
            Assert.Equal(result, _pollerResult);
        }
    }
}
