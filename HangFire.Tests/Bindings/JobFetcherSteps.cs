using System;
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

        [Given(@"the fetcher listening the queue")]
        public void GivenTheFetcherListeningTheQueue()
        {
            Given(String.Format("the fetcher listening the '{0}' queue", QueueSteps.DefaultQueue));
        }

        [Given(@"the fetcher listening the '(.+)' queue")]
        public void GivenTheFetcherListeningTheQueue(string queue)
        {
            _fetcher = new JobFetcher(Redis.Client, queue, TimeSpan.FromSeconds(1));
        }

        [When(@"it dequeues a job")]
        public void WhenItDequeuesAJob()
        {
            _jobId = _fetcher.DequeueJobId();
        }

        [Then(@"the fetcher returns the job")]
        public void ThenTheFetcherReturnsTheJob()
        {
            Assert.AreEqual(JobSteps.DefaultJobId, _jobId);
        }

        [Then(@"the fetcher returns null")]
        public void ThenTheFetcherReturnsNull()
        {
            Assert.IsNull(_jobId);
        }
    }
}
