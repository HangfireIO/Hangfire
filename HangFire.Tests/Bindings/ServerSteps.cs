using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HangFire.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ServerSteps : Steps
    {
        public const string DefaultServerName = "TestServer";

        private readonly TimeSpan _serverStartupTimeout = TimeSpan.FromMilliseconds(50);
        private JobServer _server;

        [After]
        public void TearDown()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }
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
            Redis.Client.AddItemToSet("hangfire:queues", queue);
            Redis.Client.AddItemToList(
                String.Format("hangfire:queue:{0}:dequeued", queue),
                JobSteps.DefaultJobId);
        }

        [When(@"the '(\w+)' server starts")]
        public void WhenTheServerStarts(string name)
        {
            WhenTheServerStartsWithWorkers(name, 1);
        }

        [When(@"the '(\w+)' server starts with (\d+) workers")]
        public void WhenTheServerStartsWithWorkers(string name, int workers)
        {
            CreateServer(name, workers, new [] { "critical" });
        }

        [When(@"the '(\w+)' server starts with the queues (\w+), (\w+)")]
        public void WhenTheServerStartsWithTheQueues(string name, string queue1, string queue2)
        {
            CreateServer(name, 1, new [] { queue1, queue2 });
        }

        private void CreateServer(string name, int workers, IEnumerable<string> queues)
        {
            _server = new JobServer(
                RedisFactory.BasicManager,
                name,
                workers,
                queues,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
        }

        [When(@"the '(\w+)' server shuts down")]
        public void WhenTheServerShutsDown(string name)
        {
            WhenTheServerStarts(name);
            _server.Dispose();
        }

        [Then(@"the dequeued jobs list should contain the job")]
        [Then(@"the dequeued jobs list should contain it")]
        public void ThenTheDequeuedJobsListContainsTheJob()
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}:dequeued", QueueSteps.DefaultQueue));

            CollectionAssert.Contains(jobIds, JobSteps.DefaultJobId);
        }

        [Then(@"it should be removed from the dequeued list")]
        [Then(@"the job should be removed from the dequeued list")]
        public void ThenTheDequeuedJobsListDoesNotContainTheJob()
        {
            ThenTheJobShouldBeRemovedFromTheDequeuedList(JobSteps.DefaultJobId);
        }

        [Then(@"the '(\w+)' job should be removed from the dequeued list")]
        public void ThenTheJobShouldBeRemovedFromTheDequeuedList(string jobId)
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}:dequeued", QueueSteps.DefaultQueue));

            CollectionAssert.DoesNotContain(jobIds, jobId);
        }

        [Then(@"the servers set should contain the '(\w+)' server")]
        public void ThenTheServersSetShouldContainTheServer(string name)
        {
            Thread.Sleep(_serverStartupTimeout);
            Assert.IsTrue(Redis.Client.SetContainsItem("hangfire:servers", name));
        }

        [Then(@"the servers set should not contain the '(\w+)' server")]
        public void ThenTheServersSetShouldNotContainTheServer(string name)
        {
            Thread.Sleep(_serverStartupTimeout);
            Assert.IsFalse(Redis.Client.SetContainsItem("hangfire:servers", name));
        }

        [Then(@"the '(\w+)' server's properties should contain the following items:")]
        public void ThenTheServersPropertiesShouldContainTheFollowingItems(string name, Table table)
        {
            var properties = Redis.Client.GetAllEntriesFromHash(String.Format("hangfire:server:{0}", name));
            DictionaryAssert.ContainsFollowingItems(table, properties);
        }

        [Then(@"the '(\w+)' server's queues list should contain queues (\w+), (\w+)")]
        public void ThenTheServerSQueuesListShouldContainQueues(string name, string queue1, string queue2)
        {
            var registeredQueues = Redis.Client.GetAllItemsFromList(String.Format("hangfire:server:{0}:queues", name));

            Assert.AreEqual(2, registeredQueues.Count);
            Assert.AreEqual(queue1, registeredQueues[0]);
            Assert.AreEqual(queue2, registeredQueues[1]);
        }

        [Then(@"the storage should not contain an entry for the '(\w+)' server properties")]
        public void ThenTheStorageShouldNotContainAnEntryForTheServerProperties(string name)
        {
            Assert.IsFalse(Redis.Client.ContainsKey(String.Format("hangfire:server:{0}", name)));
        }

        [Then(@"the storage should not contain an entry for the '(\w+)' server queues")]
        public void ThenTheStorageShouldNotContainAnEntryForTheServerQueues(string name)
        {
            Assert.IsFalse(Redis.Client.ContainsKey(String.Format("hangfire:server:{0}:queues", name)));
        }
    }
}
