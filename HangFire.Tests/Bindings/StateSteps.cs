using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceStack.Common.Extensions;
using ServiceStack.Redis;
using TechTalk.SpecFlow;

namespace HangFire.Tests.States
{
    [Binding]
    public class StateSteps : Steps
    {
        private JobState _state;
        private Exception _failedException;

        private Mock<JobState> _stateMock;

        [Given(@"the Succeeded state")]
        public void GivenTheSucceededState()
        {
            _state = new SucceededState("Some reason");
        }

        [Given(@"the Failed state")]
        public void GivenTheFailedState()
        {
            _failedException = new InvalidOperationException("Hello");
            _state = new FailedState("SomeReason", _failedException);
        }

        [Given(@"the Processing state")]
        public void GivenTheProcessingState()
        {
            _state = new ProcessingState("SomeReason", "TestServer");
        }

        [Given(@"the Scheduled state with the date set to tomorrow")]
        public void GivenTheScheduledStateWithTheDateSetToTomorrow()
        {
            _state = new ScheduledState("SomeReason", DateTime.UtcNow.AddDays(1));
        }

        [Given(@"the Enqueued state with the 'test' value for the 'queue' argument")]
        public void GivenTheEnqueueStateWithTheValueForTheQueueArgument()
        {
            _state = new EnqueuedState("SomeReason", "test");
        }

        [Given(@"a '(.+)' state")]
        public void GivenAState(string state)
        {
            _stateMock = new Mock<JobState>("SomeReason");
            _stateMock.Setup(x => x.StateName).Returns(state);
            _stateMock.Setup(x => x.GetProperties()).Returns(new Dictionary<string, string>());

            _state = _stateMock.Object;
        }

        [Given(@"a '(.+)' state with the following properties:")]
        public void GivenAStateWithTheFollowingProperties(string state, Table table)
        {
            Given(String.Format("a '{0}' state", state));
            _stateMock.Setup(x => x.GetProperties()).Returns(
                table.Rows.ToDictionary(x => x["Name"], x => x["Value"]));
        }

        [When(@"I apply it")]
        public void WhenIApplyIt()
        {
            using (var transaction = Redis.Client.CreateTransaction())
            {
                _state.Apply(transaction, JobSteps.DefaultJobId);
                transaction.Commit();
            }
        }

        [When(@"after I unapply it")]
        public void WhenAfterIUnapplyIt()
        {
            using (var transaction = Redis.Client.CreateTransaction())
            {
                var descriptor = StateMachine.GetStateDescriptor(_state.StateName);

                if (descriptor != null)
                {
                    descriptor.Unapply(transaction, JobSteps.DefaultJobId);
                }

                transaction.Commit();
            }
        }

        [When(@"I apply the state")]
        public void WhenIApplyTheState()
        {
            var stateMachine = new StateMachine(Redis.Client);
            stateMachine.ChangeState(JobSteps.DefaultJobId, _state);
        }

        [Then(@"the state name is equal to '(.+)'")]
        public void ThenTheStateNameIsEqualTo(string name)
        {
            Assert.AreEqual(name, _state.StateName);
        }

        [Then(@"it should expire the job")]
        public void ThenItShouldExpireTheJob()
        {
            var keys = new[]
                {
                    String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                    String.Format("hangfire:job:{0}:state", JobSteps.DefaultJobId),
                    String.Format("hangfire:job:{0}:history", JobSteps.DefaultJobId)
                };

            keys.ForEach(x =>
                {
                    var ttl = Redis.Client.GetTimeToLive(x);
                    Assert.IsTrue(
                        ttl.TotalSeconds > 0,
                        "TTL for the '{0}' key is '{1}'", x, ttl);
                });
        }

        [Then(@"it should (increase|decrease) the succeeded counter")]
        public void ThenItShouldIncreaseTheSucceededCounter(string changeType)
        {
            Assert.AreEqual(
                changeType == "increase" ? "1" : "0",
                Redis.Client.GetValue(String.Format("hangfire:stats:succeeded")));
        }

        [Then(@"the job should be added to the succeeded list")]
        public void ThenItShouldBeAddedToTheSucceededList()
        {
            Assert.AreEqual(1, Redis.Client.GetListCount("hangfire:succeeded"));
            Assert.AreEqual(JobSteps.DefaultJobId, Redis.Client.PopItemFromList(
                "hangfire:succeeded"));
        }

        [Then(@"it should persist the job")]
        public void ThenItShouldPersistTheJob()
        {
            var keys = new[]
                {
                    String.Format("hangfire:job:{0}", JobSteps.DefaultJobId),
                    String.Format("hangfire:job:{0}:state", JobSteps.DefaultJobId),
                    String.Format("hangfire:job:{0}:history", JobSteps.DefaultJobId)
                };

            keys.ForEach(x =>
            {
                var ttl = Redis.Client.GetTimeToLive(x);
                Assert.IsTrue(
                    ttl.Seconds == -1,
                    "TTL for the '{0}' key is '{1}'", x, ttl);
            });
        }

        [Then(@"the job should be removed from the succeeded list")]
        public void ThenTheJobShouldBeRemovedFromTheSucceededList()
        {
            Assert.AreEqual(0, Redis.Client.GetListCount("hangfire:succeeded"));
        }

        [Then(@"properties table contains the following items:")]
        public void ThenPropertiesTableContainsTheFollowingItems(Table table)
        {
            DictionaryAssert.ContainsFollowingItems(table, _state.GetProperties());
        }

        [Then(@"the job should be added to the failed set")]
        public void ThenTheJobShouldBeAddedToTheFailedSet()
        {
            Assert.AreEqual(1, Redis.Client.GetSortedSetCount("hangfire:failed"));
            Assert.IsTrue(Redis.Client.SortedSetContainsItem("hangfire:failed", JobSteps.DefaultJobId));
        }

        [Then(@"the job should be removed from the failed set")]
        public void ThenTheJobShouldBeRemovedFromTheFailedSet()
        {
            Assert.AreEqual(0, Redis.Client.GetSortedSetCount("hangfire:failed"));
        }

        [Then(@"the processing set contains the job")]
        public void ThenTheProcessingSetContainsTheJob()
        {
            Assert.IsTrue(Redis.Client.SortedSetContainsItem("hangfire:processing", JobSteps.DefaultJobId));
        }

        [Then(@"the processing set does not contain the job")]
        public void ThenTheProcessingSetDoesNotContainTheJob()
        {
            Assert.IsFalse(Redis.Client.SortedSetContainsItem("hangfire:processing", JobSteps.DefaultJobId));
        }

        [Then(@"processing timestamp is set to UtcNow")]
        public void ThenProcessingTimestampIsSetToUtcNow()
        {
            var score = Redis.Client.GetItemScoreInSortedSet("hangfire:processing", JobSteps.DefaultJobId);
            var timestamp = JobHelper.FromTimestamp((long)score);

            Assert.IsTrue(timestamp > DateTime.UtcNow.AddSeconds(-1));
            Assert.IsTrue(timestamp < DateTime.UtcNow.AddSeconds(1));
        }

        [Then(@"the schedule contains the job that will be enqueued tomorrow")]
        public void ThenTheScheduleContainsTheJobThatWillBeEnqueuedTomorrow()
        {
            Assert.IsTrue(Redis.Client.SortedSetContainsItem("hangfire:schedule", JobSteps.DefaultJobId));
            var score = Redis.Client.GetItemScoreInSortedSet("hangfire:schedule", JobSteps.DefaultJobId);
            var timestamp = JobHelper.FromTimestamp((long) score);

            Assert.IsTrue(timestamp >= DateTime.UtcNow.Date.AddDays(1));
            Assert.IsTrue(timestamp < DateTime.UtcNow.Date.AddDays(2));
        }

        [Then(@"the schedule does not contain the job")]
        public void ThenTheScheduleDoesNotContainTheJob()
        {
            Assert.IsFalse(Redis.Client.SortedSetContainsItem("hangfire:schedule", JobSteps.DefaultJobId));
        }

        [Then(@"the '(.+)' queue was added to the queues set")]
        public void ThenTheQueueWasAddedToTheQueuesSet(string queue)
        {
            Assert.IsTrue(Redis.Client.SetContainsItem("hangfire:queues", queue));
        }

        [Then(@"the job state is changed to '(.+)'")]
        public void ThenTheJobStateIsChangedTo(string state)
        {
            var job = Redis.Client.GetAllEntriesFromHash(String.Format("hangfire:job:{0}", JobSteps.DefaultJobId));
            Assert.AreEqual(state, job["State"]);
        }

        [Then(@"the job's state entry contains the following items:")]
        public void ThenTheJobsStateEntryContainsTheFollowingItems(Table table)
        {
            var stateEntry = Redis.Client.GetAllEntriesFromHash(
                String.Format("hangfire:job:{0}:state", JobSteps.DefaultJobId));
            DictionaryAssert.ContainsFollowingItems(table, stateEntry);
        }

        [Then(@"the last history entry contains the following items:")]
        public void ThenTheHistoryEntryShouldContainTheFollowingItems(Table table)
        {
            var entry = Redis.Client.RemoveStartFromList(
                String.Format("hangfire:job:{0}:history", JobSteps.DefaultJobId));
            Assert.IsNotNull(entry);

            var history = JobHelper.FromJson<Dictionary<string, string>>(entry);
            Assert.IsNotNull(history, entry);
            
            DictionaryAssert.ContainsFollowingItems(table, history);
        }

        [Then(@"Apply method has called")]
        public void ThenApplyMethodHasCalled()
        {
            _stateMock.Verify(
                x => x.Apply(It.Is<IRedisTransaction>(y => y != null), It.Is<string>(y => y == JobSteps.DefaultJobId)), 
                Times.Once);
        }
    }
}
