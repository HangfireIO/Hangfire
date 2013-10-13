using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ServerSteps : Steps
    {
        public const string DefaultServerName = "TestServer";

        [Given(@"a server processing the default queue")]
        public void GivenServerProcessingTheDefaultQueue()
        {
            Redis.Client.AddItemToSet(
                String.Format("hangfire:server:{0}:queues", DefaultServerName),
                QueueSteps.DefaultQueueName);
        }

        [Given(@"a fetched job")]
        public void AFetchedJob()
        {
            Given("a job");

            Redis.Client.AddItemToList(
                String.Format("hangfire:server:{0}:fetched:{1}", DefaultServerName, QueueSteps.DefaultQueueName),
                JobSteps.DefaultJobId);
        }

        [Then(@"the fetched jobs queue still contains the job")]
        public void TheFetchedJobsQueueContainsTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:server:{0}:fetched:{1}", DefaultServerName, QueueSteps.DefaultQueueName));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"the fetched jobs queue does not contain the job")]
        public void TheFetchedJobsQueueDoesNotContainTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:server:{0}:fetched:{1}", DefaultServerName, QueueSteps.DefaultQueueName));

            CollectionAssert.DoesNotContain(jobIds, JobSteps.DefaultJobId);
        }
    }
}
