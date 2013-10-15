using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class QueueSteps : Steps
    {
        public const string DefaultQueue = "default";
        
        [Given(@"an empty queue")]
        public void GivenAnEmptyQueue()
        {
        }

        [Given(@"an enqueued job")]
        public void GivenAnEnqueuedJob()
        {
            Given(String.Format("a job in the '{0}' queue", DefaultQueue));
        }

        [Given(@"a job in the '(.+)' queue")]
        public void GivenAJobInTheQueue(string queue)
        {
            Given("a job");

            Redis.Client.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", queue),
                JobSteps.DefaultJobId);
        }

        [Given(@"the '(.+)' job in the queue")]
        public void GivenTheJobInTheQueue(string jobId)
        {
            Given(String.Format("the '{0}' job in the '{1}' queue", jobId, DefaultQueue));
        }

        [Given(@"the '(.+)' job in the '(.+)' queue")]
        public void GivenTheJobInTheQueue(string jobId, string queue)
        {
            Given(String.Format("the '{0}' job", jobId));

            Redis.Client.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", queue),
                jobId);
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

        [Then(@"the queue does not contain the job anymore")]
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

        [Then(@"the '(.+)' queue is empty")]
        public void ThenTheQueueIsEmpty(string queue)
        {
            var length = Redis.Client.GetListCount(
                String.Format("hangfire:queue:{0}", queue));
            Assert.AreEqual(0, length);
        }

        [Then(@"the '(.+)' queue length is (\d+)")]
        public void ThenTheQueueLengthIs(string queue, int length)
        {
            var actualLength = Redis.Client.GetListCount(
                String.Format("hangfire:queue:{0}", queue));
            Assert.AreEqual(length, actualLength);
        }
    }
}