using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ServerSteps : Steps
    {
        public const string DefaultServerName = "TestServer";

        [Given(@"a server processing the '(.+)' queue")]
        public void GivenServerProcessingTheQueue(string queueName)
        {
            Redis.Client.AddItemToSet(
                String.Format("hangfire:server:{0}:queues", DefaultServerName),
                queueName);
        }

        [Given(@"a dequeued job")]
        public void GivenADequeuedJob()
        {
            Given("a job");
            Given("the job was dequeued");
        }

        [Given(@"a dequeued job of the '(.+)' type")]
        public void GivenADequeuedJobOfTheType(string type)
        {
            Given(String.Format("a job of the '{0}' type", type));
            Given("the job was dequeued");
        }

        [Given(@"a dequeued job from the '(.+)' queue")]
        public void GivenADequeuedJobFromTheQueue(string queueName)
        {
            Given("a job");
            Given(String.Format("the job was dequeued from the '{0}' queue", queueName));
        }

        [Given(@"the job was dequeued")]
        public void GivenTheJobWasDequeued()
        {
            Given(String.Format("the job was dequeued from the '{0}' queue", QueueSteps.DefaultQueueName));
        }

        [Given(@"the job was dequeued from the '(.+)' queue")]
        public void GivenTheJobWasDequeuedFromTheQueue(string queueName)
        {
            Redis.Client.AddItemToList(
                String.Format("hangfire:server:{0}:dequeued:{1}", DefaultServerName, queueName),
                JobSteps.DefaultJobId);
        }

        [Then(@"the dequeued jobs queue still contains the job")]
        public void ThenTheDequeuedJobsQueueContainsTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:server:{0}:dequeued:{1}", DefaultServerName, QueueSteps.DefaultQueueName));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"the dequeued jobs queue does not contain the job anymore")]
        public void ThenTheDequeuedJobsQueueDoesNotContainTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:server:{0}:dequeued:{1}", DefaultServerName, QueueSteps.DefaultQueueName));

            CollectionAssert.DoesNotContain(jobIds, JobSteps.DefaultJobId);
        }
    }
}
