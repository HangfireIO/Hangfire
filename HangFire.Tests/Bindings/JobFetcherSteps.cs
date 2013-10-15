using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobFetcherSteps : Steps
    {
        private JobFetcher _fetcher;
        private string _jobId;
        private IList<string> _queues;

        [Given(@"the fetcher listening the queue")]
        public void GivenTheFetcherListeningTheQueue()
        {
            Given(String.Format("the fetcher listening the '{0}' queue", QueueSteps.DefaultQueue));
        }

        [Given(@"the fetcher listening the '(.+)' queue")]
        public void GivenTheFetcherListeningTheQueue(string queue)
        {
            _fetcher = new JobFetcher(Redis.Client, new List<string> { queue }, TimeSpan.FromSeconds(1));
        }

        [Given(@"the following queues:")]
        public void GivenTheFollowingQueues(Table table)
        {
            foreach (var row in table.Rows)
            {
                for (var i = 0; i < int.Parse(row["Jobs"]); i++)
                {
                    Given(String.Format("a job in the '{0}' queue", row["Queue"]));
                }
            }

            _queues = table.Rows.Select(x => x["Queue"]).ToList();
        }

        [Given(@"the fetcher listening them")]
        public void GivenTheFetcherListeningThem()
        {
            _fetcher = new JobFetcher(Redis.Client, _queues, TimeSpan.FromSeconds(1));
        }

        [When(@"it dequeues a job.*")]
        public void WhenItDequeuesAJob()
        {
            _jobId = _fetcher.DequeueJobId();
        }

        [When(@"it dequeues (\d+) jobs?")]
        public void WhenItDequeuesJobs(int count)
        {
            for (var i = 0; i < count; i++)
            {
                When("it dequeues a job");
            }
        }

        [Then(@"the fetcher returns the job")]
        public void ThenTheFetcherReturnsTheJob()
        {
            Assert.AreEqual(JobSteps.DefaultJobId, _jobId);
        }

        [Then(@"the fetcher returns the '(.+)' job")]
        public void ThenTheFetcherReturnsTheJob(string jobId)
        {
            Assert.AreEqual(jobId, _jobId);
        }

        [Then(@"the fetcher returns null")]
        public void ThenTheFetcherReturnsNull()
        {
            Assert.IsNull(_jobId);
        }

        [Then(@"all queues are empty")]
        public void ThenAllQueuesAreEmpty()
        {
            foreach (var queue in _queues)
            {
                Then(String.Format("the '{0}' queue is empty", queue));
            }
        }
    }
}
