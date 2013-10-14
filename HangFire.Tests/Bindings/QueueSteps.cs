using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class QueueSteps : Steps
    {
        public const string DefaultQueue = "default";

        [Given(@"a job in the '(.+)' queue")]
        public void GivenAJobInTheQueue(string queue)
        {
            Given("a job");

            Redis.Client.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", DefaultQueue),
                JobSteps.DefaultJobId);
        }

        [Then(@"the queue contains the job")]
        public void ThenTheQueueContainsTheJob()
        {
            Then(String.Format("the '{0}' queue contains the job", DefaultQueue));
        }

        [Then(@"the '(.+)' queue contains the job")]
        public void ThenTheQueueContainsTheJob(string queue)
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}", queue));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"the queue does not contain the job")]
        public void ThenTheQueueDoesNotContainTheJob()
        {
            Then(String.Format("the '{0}' queue does not contain the job", DefaultQueue));
        }

        [Then(@"the '(.+)' queue does not contain the job")]
        public void ThenTheQueueDoesNotContainTheJob(string queue)
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}", queue));

            CollectionAssert.DoesNotContain(jobIds, JobSteps.DefaultJobId);
        }
    }
}