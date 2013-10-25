using System;
using System.Collections.Generic;
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

        [When(@"I call the `(.+)`")]
        public void WhenICallThe(string code)
        {
            try
            {
                if (code.Equals("Perform.Async<TestJob>()"))
                {
                    _jobId = Perform.Async<TestJob>();
                }
                else if (code.Equals("Perform.Async<TestJob>(new { ArticleId = 3, Author = \"odinserj\" })"))
                {
                    _jobId = Perform.Async<TestJob>(new { ArticleId = 3, Author = "odinserj" });
                }
                else if (code.Equals("Perform.Async(typeof(TestJob))"))
                {
                    _jobId = Perform.Async(typeof (TestJob));
                }
                else if (code.Equals("Perform.Async(null)"))
                {
                    _jobId = Perform.Async(null);
                }
                else if (code.Equals("Perform.Async(typeof(TestJob), new { ArticleId = 3 })"))
                {
                    _jobId = Perform.Async(typeof (TestJob), new { ArticleId = 3 });
                }
                else if (code.Equals("Perform.Async(null, new { ArticleId = 3 })"))
                {
                    _jobId = Perform.Async(null, new { ArticleId = 3 });
                }
                else if (code.Equals("Perform.In<TestJob>(TimeSpan.FromDays(1))"))
                {
                    _jobId = Perform.In<TestJob>(TimeSpan.FromDays(1));
                }
                else if (code.Equals("Perform.In<TestJob>(TimeSpan.FromDays(1), new { ArticleId = 3 })"))
                {
                    _jobId = Perform.In<TestJob>(TimeSpan.FromDays(1), new { ArticleId = 3 });
                }
                else if (code.Equals("Perform.In(TimeSpan.FromDays(1), typeof(TestJob))"))
                {
                    _jobId = Perform.In(TimeSpan.FromDays(1), typeof (TestJob));
                }
                else if (code.Equals("Perform.In(TimeSpan.FromDays(1), typeof(TestJob), new { ArticleId = 3 })"))
                {
                    _jobId = Perform.In(TimeSpan.FromDays(1), typeof (TestJob), new { ArticleId = 3 });
                }
                else if (code.Equals("Perform.Async<CriticalQueueJob>()"))
                {
                    _jobId = Perform.Async<CriticalQueueJob>();
                }
                else if (code.Equals("Perform.Async<InvalidQueueJob>()"))
                {
                    _jobId = Perform.Async<InvalidQueueJob>();
                }
                else if (code.Equals("Perform.Async<EmptyQueueJob>()"))
                {
                    _jobId = Perform.Async<EmptyQueueJob>();
                }
                else
                {
                    ScenarioContext.Current.Pending();
                }
            }
            catch (PendingStepException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
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
