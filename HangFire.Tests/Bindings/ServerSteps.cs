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
            Redis.Client.AddItemToSet("hangfire:queues", queue);
            Redis.Client.AddItemToList(
                String.Format("hangfire:queue:{0}:dequeued", queue),
                JobSteps.DefaultJobId);
        }

        [Then(@"the dequeued jobs list still contains the job")]
        public void ThenTheDequeuedJobsListContainsTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}:dequeued", QueueSteps.DefaultQueue));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"the dequeued jobs list does not contain the job anymore")]
        public void ThenTheDequeuedJobsListDoesNotContainTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}:dequeued", QueueSteps.DefaultQueue));

            CollectionAssert.DoesNotContain(jobIds, JobSteps.DefaultJobId);
        }
    }
}
