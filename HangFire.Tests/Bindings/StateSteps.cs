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

        private IDictionary<string, Mock<JobState>> _stateMocks
            = new Dictionary<string, Mock<JobState>>(); 

        private Mock<JobStateDescriptor> _oldStateDescriptorMock;

        private readonly IDictionary<string, JobStateDescriptor> _descriptors
            = new Dictionary<string, JobStateDescriptor>();
        private readonly IList<IStateChangedFilter> _stateChangingFilters 
            = new List<IStateChangedFilter>();
        private readonly IList<IStateAppliedFilter> _stateAppliedFilters
            = new List<IStateAppliedFilter>();

        private readonly IList<string> _stateChangingResults = new List<string>(); 
        private readonly IList<string> _stateAppliedResults = new List<string>(); 

        private IDictionary<string, string> _stateProperties;
            
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
            var mock = new Mock<JobState>("SomeReason");
            mock.Setup(x => x.StateName).Returns(state);
            mock.Setup(x => x.GetProperties()).Returns(new Dictionary<string, string>());

            _stateMocks.Add(state, mock);
        }

        [Given(@"a '(.+)' state with the following properties:")]
        public void GivenAStateWithTheFollowingProperties(string state, Table table)
        {
            Given(String.Format("a '{0}' state", state));

            _stateProperties = table.Rows.ToDictionary(x => x["Name"], x => x["Value"]);
            _stateMocks[state].Setup(x => x.GetProperties()).Returns(_stateProperties);
        }

        [Given(@"a job in the 'Old' state with registered descriptor")]
        public void GivenAJobInTheStateWithRegisteredDescriptor()
        {
            Given("a job");
            Given("its state is Old");

            _oldStateDescriptorMock = new Mock<JobStateDescriptor>();
            _descriptors.Add("Old", _oldStateDescriptorMock.Object);
        }

        [Given(@"a state changing filter '([a-zA-Z]+)'")]
        public void GivenAStateChangingFilter(string name)
        {
            _stateChangingFilters.Add(new TestStateChangingFilter(name, _stateChangingResults));
        }

        [Given(@"a state changing filter '(\w+)' that changes the state to the '(\w+)'")]
        public void GivenAStateChangingFilterThatChangesTheStateToThe(string name, string state)
        {
            Given(String.Format("a '{0}' state", state));

            _stateChangingFilters.Add(
                new TestStateChangingFilter(name, _stateChangingResults, _stateMocks[state].Object));
        }

        [Given(@"a state applied filter '(\w+)'")]
        public void GivenAStateAppliedFilter(string name)
        {
            _stateAppliedFilters.Add(new TestStateAppliedFilter(name, _stateAppliedResults));
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
                if (StateMachine.Descriptors.ContainsKey(_state.StateName))
                {
                    StateMachine.Descriptors[_state.StateName]
                        .Unapply(transaction, JobSteps.DefaultJobId);
                }

                transaction.Commit();
            }
        }

        [When(@"I change the state of the job")]
        public void WhenIApplyTheState()
        {
            var stateMachine = new StateMachine(
                Redis.Client, _descriptors, _stateChangingFilters, _stateAppliedFilters);
            stateMachine.ChangeState(JobSteps.DefaultJobId, _state);
        }

        [When(@"I change the state of the job to the '(\w+)'")]
        public void WhenIChangeTheStateOfTheJobToThe(string state)
        {
            When(String.Format(
                "I change the state of the '{0}' job to the '{1}'",
                JobSteps.DefaultJobId,
                state));
        }

        [When(@"I change the state of the '(\w+)' job to the '(\w+)'")]
        public void WhenIChangeTheStateOfTheJobToThe(string jobId, string state)
        {
            var stateMachine = new StateMachine(
                Redis.Client, _descriptors, _stateChangingFilters, _stateAppliedFilters);
            stateMachine.ChangeState(jobId, _stateMocks[state].Object);
        }

        [When(@"I change the state of the job to the '(\w+)' allowing only transition from the '(\w+)' state")]
        public void WhenIChangeTheStateOfTheJobToTheStateAllowedTransitions(
            string state, string allowedState)
        {
            var stateMachine = new StateMachine(
                Redis.Client, _descriptors, _stateChangingFilters, _stateAppliedFilters);
            stateMachine.ChangeState(JobSteps.DefaultJobId, _stateMocks[state].Object, allowedState);
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

        [Then(@"the '(\w+)' state was applied to the job")]
        public void ThenApplyMethodHasCalled(string state)
        {
            _stateMocks[state].Verify(
                x => x.Apply(It.Is<IRedisTransaction>(y => y != null), It.Is<string>(y => y == JobSteps.DefaultJobId)), 
                Times.Once);
        }

        [Then(@"the '(\w+)' state was not applied to the job")]
        public void ThenTheStateWasNotAppliedToTheJob(string state)
        {
            _stateMocks[state].Verify(
                x => x.Apply(It.IsAny<IRedisTransaction>(), It.IsAny<string>()),
                Times.Never);
        }

        [Then(@"the old state was unapplied")]
        public void ThenTheOldStateWasUnapplied()
        {
            _oldStateDescriptorMock.Verify(
                x => x.Unapply(It.Is<IRedisTransaction>(y => y != null), It.Is<string>(y => y == JobSteps.DefaultJobId)));
        }

        [Then(@"the old state was not unapplied")]
        public void ThenTheOldStateWasNotUnapplied()
        {
            _oldStateDescriptorMock.Verify(
                x => x.Unapply(It.IsAny<IRedisTransaction>(), It.IsAny<string>()),
                Times.Never);
        }

        [Then(@"the last history entry should contain all of the above properties")]
        public void ThenTheHistoryRecordShouldContainProperties()
        {
            var entry = Redis.Client.RemoveStartFromList(
                String.Format("hangfire:job:{0}:history", JobSteps.DefaultJobId));
            Assert.IsNotNull(entry);

            var history = JobHelper.FromJson<Dictionary<string, string>>(entry);
            Assert.IsNotNull(history, entry);

            foreach (var property in _stateProperties)
            {
                Assert.IsTrue(history.ContainsKey(property.Key));
                Assert.AreEqual(property.Value, history[property.Key]);
            }
        }

        [Then(@"the state entry should contain all of the above properties")]
        public void ThenTheStateEntryShouldContainAllOfTheAboveProperties()
        {
            var stateEntry = Redis.Client.GetAllEntriesFromHash(
                String.Format("hangfire:job:{0}:state", JobSteps.DefaultJobId));

            foreach (var property in _stateProperties)
            {
                Assert.IsTrue(stateEntry.ContainsKey(property.Key));
                Assert.AreEqual(property.Value, stateEntry[property.Key]);
            }
        }

        [Then(@"changing filters were executed in the following order:")]
        public void ThenChangingFiltersWereExecutedInTheFollowingOrder(Table table)
        {
            Assert.AreEqual(table.RowCount, _stateChangingResults.Count);

            for (var i = 0; i < table.RowCount; i++)
            {
                Assert.AreEqual(table.Rows[i]["Filter"], _stateChangingResults[i]);
            }
        }

        [Then(@"changing filters were not executed")]
        public void ThenChangingFiltersWereNotExecuted()
        {
            Assert.AreEqual(0, _stateChangingResults.Count);
        }

        [Then(@"the history for following states were added:")]
        public void ThenTheHistoryForFollowingStatesWereAdded(Table table)
        {
            var serializedHistory = Redis.Client.GetAllItemsFromList(
                String.Format("hangfire:job:{0}:history", JobSteps.DefaultJobId));
            var history = serializedHistory.Select(JobHelper.FromJson<Dictionary<string, string>>).ToList();

            for (var i = 0; i < table.RowCount; i++)
            {
                Assert.AreEqual(table.Rows[i]["State"], history[i]["State"]);
            }
        }

        [Then(@"state applied filter methods were executed in the following order:")]
        public void ThenStateAppliedFilterMethodsWereExecutedInTheFollowingOrder(Table table)
        {
            Assert.AreEqual(table.RowCount, _stateAppliedResults.Count);
            
            for (var i = 0; i < table.RowCount; i++)
            {
                Assert.AreEqual(table.Rows[i]["Method"], _stateAppliedResults[i]);
            }
        }
    }
}
