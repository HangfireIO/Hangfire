using System;
using System.Collections.Generic;
using HangFire.Client;
using HangFire.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ClientSteps
    {
        private string _jobId;
        private Exception _exception;

        [Given(@"the following job type:")]
        public void GivenTheJobType(string typeDefinition)
        {
        }

        [Given(@"the custom types:")]
        public void GivenTheCustomTypes(string typeDefinition)
        {
        }

        [Then(@"the argumentless '(\w+)' should be created")]
        public void ThenTheArgumentlessJobShouldBeCreated(string type)
        {
            var job = Redis.Client.GetAllEntriesFromHash(String.Format("hangfire:job:{0}", _jobId));
            Assert.AreNotEqual(0, job.Count);
            Assert.IsTrue(job["Type"].Contains(type));

            var args = JobHelper.FromJson<Dictionary<string, string>>(job["Args"]);
            Assert.AreEqual(0, args.Count);
        }

        [Then(@"it should be enqueued to the default queue")]
        public void ThenItShouldBeEnqueuedToTheDefaultQueue()
        {
            ThenItShouldBeEnqueuedToTheQueue(QueueSteps.DefaultQueue);
        }

        [Then(@"it should be enqueued to the '(\w+)' queue")]
        public void ThenItShouldBeEnqueuedToTheQueue(string name)
        {
            var jobIds = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:queue:{0}", name));

            Assert.AreEqual(1, jobIds.Count);
            Assert.AreEqual(_jobId, jobIds[0]);
        }

        [Then(@"it should be scheduled for tomorrow")]
        public void ThenItShouldBeScheduledForTomorrow()
        {
            Assert.IsTrue(Redis.Client.SortedSetContainsItem("hangfire:schedule", _jobId));
            var score = Redis.Client.GetItemScoreInSortedSet("hangfire:schedule", _jobId);
            var timestamp = JobHelper.FromTimestamp((long) score);

            Assert.IsTrue(DateTime.UtcNow.Date.AddDays(1) <= timestamp);
            Assert.IsTrue(timestamp < DateTime.UtcNow.Date.AddDays(2));
        }

        [Then(@"a '(.+)' should be thrown")]
        public void AnExceptionShouldBeThrown(string exceptionType)
        {
            Assert.IsNotNull(_exception);
            Assert.IsInstanceOfType(_exception, Type.GetType(exceptionType, true));
        }

        [Then(@"a CreateJobFailedException should be thrown")]
        public void ACreateJobFailedExceptionShouldBeThrown()
        {
            Assert.IsNotNull(_exception);
            Assert.IsInstanceOfType(_exception, typeof(CreateJobFailedException));
        }

        [Then(@"the '(\w+)' should be created with the following arguments:")]
        public void ThenTheJobShouldBeCreatedWithTheFollowingArguments(string type, Table table)
        {
            var job = Redis.Client.GetAllEntriesFromHash(String.Format("hangfire:job:{0}", _jobId));
            Assert.AreNotEqual(0, job.Count);
            Assert.IsTrue(job["Type"].Contains(type));

            var args = JobHelper.FromJson<Dictionary<string, string>>(job["Args"]);
            DictionaryAssert.ContainsFollowingItems(table, args);
        }

        [Then(@"the argumentless '(\w+)' should be added to the default queue")]
        public void ThenTheArgumentlessJobShouldBeAddedToTheDefaultQueue(string type)
        {
            ThenTheArgumentlessJobShouldBeCreated(type);
            ThenItShouldBeEnqueuedToTheDefaultQueue();
        }

        [Then(@"the '(\w+)' should be added to the default queue with the following arguments:")]
        public void ThenTheJobShouldBeAddedToTheDefaultQueueWithArguments(string type, Table table)
        {
            ThenTheJobShouldBeCreatedWithTheFollowingArguments(type, table);
            ThenItShouldBeEnqueuedToTheDefaultQueue();
        }

        [Then(@"the argumentless '(\w+)' should be scheduled for tomorrow")]
        public void ThenTheArgumentlessJobShouldBeScheduledForTomorrow(string type)
        {
            ThenTheArgumentlessJobShouldBeCreated(type);
            ThenItShouldBeScheduledForTomorrow();
        }

        [Then(@"the '(\w+)' should be scheduled for tomorrow with the following arguments:")]
        public void ThenTheJobShouldBeScheduledForTomorrowWithArguments(string type, Table table)
        {
            ThenTheJobShouldBeCreatedWithTheFollowingArguments(type, table);
            ThenItShouldBeScheduledForTomorrow();
        }

        [Then(@"the argumentless '(\w+)' should be added to the '(\w+)' queue")]
        public void ThenTheArgumentlessJobShouldBeAddedToTheQueue(string type, string queue)
        {
            ThenTheArgumentlessJobShouldBeCreated(type);
            ThenItShouldBeEnqueuedToTheQueue(queue);
        }
    }
}
