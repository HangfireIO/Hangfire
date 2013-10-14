using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ServerSteps : Steps
    {
        public const string DefaultServerName = "TestServer";
        public const string DefaultInstanceId = "some-server-id";

        [Given(@"a server processing the '(.+)' queue")]
        public void GivenServerProcessingTheQueue(string queue)
        {
            Redis.Client.AddItemToSet("hangfire:servers", DefaultServerName);
            Redis.Client.AddItemToSet(
                String.Format("hangfire:server:{0}:instances", DefaultServerName),
                DefaultInstanceId);
            Redis.Client.AddItemToSet(
                String.Format("hangfire:server:{0}:instance:{1}:queues", DefaultServerName, DefaultInstanceId),
                queue);
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
        public void GivenADequeuedJobFromTheQueue(string queue)
        {
            Given("a job");
            Given(String.Format("the job was dequeued from the '{0}' queue", queue));
        }

        [Given(@"the job was dequeued")]
        public void GivenTheJobWasDequeued()
        {
            Given(String.Format("the job was dequeued from the '{0}' queue", QueueSteps.DefaultQueue));
        }

        [Given(@"the job was dequeued from the '(.+)' queue")]
        public void GivenTheJobWasDequeuedFromTheQueue(string queue)
        {
            Redis.Client.AddItemToList(
                String.Format("hangfire:server:{0}:dequeued:{1}", DefaultServerName, queue),
                JobSteps.DefaultJobId);
        }

        [Then(@"the dequeued jobs queue still contains the job")]
        public void ThenTheDequeuedJobsQueueContainsTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:server:{0}:dequeued:{1}", DefaultServerName, QueueSteps.DefaultQueue));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"the dequeued jobs queue does not contain the job anymore")]
        public void ThenTheDequeuedJobsQueueDoesNotContainTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:server:{0}:dequeued:{1}", DefaultServerName, QueueSteps.DefaultQueue));

            CollectionAssert.DoesNotContain(jobIds, JobSteps.DefaultJobId);
        }
    }
}
