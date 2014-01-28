using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HangFire.Server.Fetching;
using HangFire.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobFetcherSteps : Steps
    {
        private JobFetcher _fetcher;
        private QueuedJob _job;
        private IList<string> _queues;

        private Exception _exception;

        [Given(@"the fetcher listening the queue")]
        public void GivenTheFetcherListeningTheQueue()
        {
            Given(String.Format("the fetcher listening the '{0}' queue", QueueSteps.DefaultQueue));
        }

        [Given(@"the fetcher listening the '(.+)' queue")]
        public void GivenTheFetcherListeningTheQueue(string queue)
        {
            _fetcher = new JobFetcher(RedisFactory.BasicManager, queue, TimeSpan.FromSeconds(1));
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

        [When(@"it dequeues a job.*")]
        public void WhenItDequeuesAJob()
        {
            var cts = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100)); 
                    cts.Cancel();
                });

            try
            {
                _job = _fetcher.DequeueJob(cts.Token);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [When(@"it dequeues (\d+) jobs?")]
        public void WhenItDequeuesJobs(int count)
        {
            for (var i = 0; i < count; i++)
            {
                When("it dequeues a job");
            }
        }

        [Then(@"the fetcher should return the payload")]
        public void ThenTheFetcherReturnsTheJob()
        {
            Assert.IsNotNull(_job);
            Assert.AreEqual(JobSteps.DefaultJobId, _job.Payload.Id);
            
        }


        [Then(@"the fetcher should return the '(.+)' job")]
        public void ThenTheFetcherReturnsTheJob(string jobId)
        {
            Assert.AreEqual(jobId, _job.Payload.Id);
        }

        [Then(@"the fetcher should not return any job")]
        public void ThenTheFetcherDoesNotReturnAnyJob()
        {
            Assert.IsNotNull(_exception);
            Assert.AreEqual(typeof(OperationCanceledException).Name, _exception.GetType().Name);
        }

        [Then(@"all queues should be empty")]
        public void ThenAllQueuesAreEmpty()
        {
            foreach (var queue in _queues)
            {
                Then(String.Format("the '{0}' queue is empty", queue));
            }
        }
    }
}
