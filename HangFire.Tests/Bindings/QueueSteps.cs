using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class QueueSteps : Steps
    {
        public const string DefaultQueueName = "default";

        [Given(@"a job in the (.+) queue")]
        public void GivenAJobInTheQueue(string queueName)
        {
            Given("a job");

            Redis.Client.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", DefaultQueueName),
                JobSteps.DefaultJobId);
        }

        [Then(@"the '(.+)' queue contains the job")]
        public void TheQueueContainsTheJob(string queueName)
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}", queueName));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"the '(.+)' queue does not contain the job")]
        public void TheQueueDoesNotContainTheJob(string queueName)
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}", queueName));

            CollectionAssert.DoesNotContain(jobIds, JobSteps.DefaultJobId);
        }
    }
}