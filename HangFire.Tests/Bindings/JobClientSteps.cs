using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Redis;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceStack.Redis;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobClientSteps : Steps
    {
        private JobClient _client;
        private Mock<JobState> _stateMock;
        private IDictionary<string, string> _arguments = new Dictionary<string, string>();
        private Exception _exception;

        private readonly IList<object> _filters = new List<object>();
        private readonly IList<string> _clientFilterResults = new List<string>();
        private readonly IList<string> _exceptionFilterResults = new List<string>();

        private IDictionary<string, string> _parameters;
            
        [Given("a client")]
        public void GivenAClient()
        {
            _client = new JobClient(
                new RedisStorageConnection(Redis.Storage.BasicManager.GetClient()),
                new JobCreator(_filters));
        }

        [Given("the client filter '(.+)'")]
        public void GivenTheClientFilter(string name)
        {
            _filters.Add(new TestFilter(name, _clientFilterResults));
        }

        [Given("the client filter '(.+)' that cancels the job")]
        public void GivenTheClientFilterThatCancelsTheJob(string name)
        {
            _filters.Add(new TestFilter(name, _clientFilterResults, false, true));
        }

        [Given("the client filter '(.+)' that handles an exception")]
        public void GivenTheClientFilterThatHandlesAnException(string name)
        {
            _filters.Add(new TestFilter(name, _clientFilterResults, false, false, true));
        }

        [Given("the client filter '(.+)' that throws an exception")]
        public void GivenTheClientFilterThatThrowsAnException(string name)
        {
            _filters.Add(new TestFilter(name, _clientFilterResults, true, false, false));
        }

        [Given("the client filter '(.+)' that sets the following parameters in the OnCreating method:")]
        public void GivenTheFilterThetSetsTheFollowingParametersInTheOnCreatingMethod(string name, Table table)
        {
            _parameters = table.Rows.ToDictionary(x => x["Name"], x => x["Value"]);

            _filters.Add(new TestFilter(
                name, 
                _clientFilterResults, 
                setOnPreMethodParameters: _parameters));
        }

        [Given(@"the client filter '(\w+)' that reads all of the above parameters")]
        public void GivenTheClientFilterThatReadsAllOfTheAboveParameters(string name)
        {
            _filters.Add(new TestFilter(
                name, _clientFilterResults, readParameters: _parameters));
        }

        [Given(@"the client filter '(\w+)' that gets the following parameters in the OnCreating method:")]
        public void GivenTheClientFilterThatGetsTheFollowingParametersInTheOnCreatingMethod(string name, Table table)
        {
            _parameters = table.Rows.ToDictionary(x => x["Name"], x => x["Value"]);
            GivenTheClientFilterThatReadsAllOfTheAboveParameters(name);
        }

        [Given(@"the client filter '(\w+)' that sets the following parameters in the OnCreated method:")]
        public void GivenTheClientFilterThatSetsTheFollowingParametersInTheOnCreatedMethod(string name, Table table)
        {
            _parameters = table.Rows.ToDictionary(x => x["Name"], x => x["Value"]);
            _filters.Add(new TestFilter(
                name, _clientFilterResults, setOnPostMethodParameters: _parameters));
        }

        [Given("the exception filter '(.+)'")]
        public void GivenTheExceptionFilter(string name)
        {
            _filters.Add(new TestExceptionFilter(name, _exceptionFilterResults));
        }

        [Given("the exception filter '(.+)' that handles an exception")]
        public void GivenTheExceptionFilterThatHandlesAnException(string name)
        {
            _filters.Add(new TestExceptionFilter(name, _exceptionFilterResults, true));
        }

        [Given(@"there is a buggy filter \(for example\)")]
        public void WhenThereWasAnExceptionWhileCreatingAJob()
        {
            _filters.Add(new TestFilter("buggy", _clientFilterResults, true));
        }
        
        [When("I create a job")]
        [When("I create an argumentless job")]
        public void WhenICreateAJob()
        {
            _stateMock = new Mock<JobState>("SomeReason");
            _stateMock.Setup(x => x.StateName).Returns("Test");
            _stateMock.Setup(x => x.GetProperties(It.IsAny<JobMethod>()))
                .Returns(new Dictionary<string, string>());

            try
            {
                _client.CreateJob(
                    JobSteps.DefaultJobId,
                    typeof(TestJob),
                    _stateMock.Object,
                    _arguments);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [When("I create a job with the following arguments:")]
        public void WhenICreateAJobWithTheFollowingArguments(Table table)
        {
            _arguments = table.Rows.ToDictionary(x => x["Name"], x => x["Value"]);
            When("I create a job");
        }

        [When("I create a job with an empty id")]
        public void WhenICreateAJobWithAnEmptyId()
        {
            try
            {
                _client.CreateJob(null, typeof(TestJob), new Mock<JobState>("1").Object, new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [When("I create a job with null type")]
        public void WhenICreateAJobWithNullType()
        {
            try
            {
                _client.CreateJob(JobSteps.DefaultJobId, null, new Mock<JobState>("1").Object, new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [When("I create a job with an empty state")]
        public void WhenICreateAJobWithAnEmptyState()
        {
            try
            {
                _client.CreateJob(JobSteps.DefaultJobId, typeof(TestJob), null, new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [When("I create a job with the incorrect type")]
        public void WhenICreateAJobWithTheIncorrectType()
        {
            try
            {
                _client.CreateJob(JobSteps.DefaultJobId, typeof(JobClientSteps), new Mock<JobState>("1").Object, new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [When("I create a job with a null dictionary as arguments")]
        public void WhenICreateAJobWithANullDictionaryAsArguments()
        {
            try
            {
                _client.CreateJob(JobSteps.DefaultJobId, typeof(TestJob), new Mock<JobState>("1").Object, null);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [Then("the storage should contain the job")]
        public void ThenTheStorageContainsIt()
        {
            Assert.IsTrue(Redis.Client.ContainsKey("hangfire:job:" + JobSteps.DefaultJobId));
        }

        [Then("the storage should not contain the job")]
        public void ThenTheStorageDoesNotContainTheJob()
        {
            Assert.IsFalse(Redis.Client.ContainsKey("hangfire:job:" + JobSteps.DefaultJobId));
        }

        [Then("it should have the following parameters:")]
        public void ThenItHasTheFollowingParameters(Table table)
        {
            var job = Redis.Client.GetAllEntriesFromHash("hangfire:job:" + JobSteps.DefaultJobId);
            DictionaryAssert.ContainsFollowingItems(table, job);
        }

        [Then("the job should contain all of the above arguments in the JSON format")]
        public void ThenTheJobContainsAllOfTheAboveArguments()
        {
            var argsJson = Redis.Client.GetValueFromHash(
                "hangfire:job:" + JobSteps.DefaultJobId,
                "Args");
            var args = JobHelper.FromJson<Dictionary<string, string>>(argsJson);

            Assert.AreEqual(_arguments.Count, args.Count);
            foreach (var pair in _arguments)
            {
                Assert.IsTrue(args.ContainsKey(pair.Key));
                Assert.AreEqual(_arguments[pair.Key], pair.Value);
            }
        }

        [Then("the given state should be applied to it")]
        public void ThenTheGivenStateWasAppliedToIt()
        {
            _stateMock.Verify(
                x => x.Apply(It.Is<StateApplyingContext>(y => y.JobId == JobSteps.DefaultJobId)),
                Times.Once);
        }

        [Then("a '(.+)' should be thrown by the client")]
        public void ThenAnExceptionIsThrown(string exceptionType)
        {
            Assert.IsNotNull(_exception);
            Assert.IsInstanceOfType(_exception, Type.GetType(exceptionType, true));
        }

        [Then("the CreateJobFailedException should be thrown by the client")]
        public void ThenTheCreateJobFailedExceptionWasThrown()
        {
            Assert.IsNotNull(_exception);
            Assert.IsInstanceOfType(_exception, typeof(CreateJobFailedException));
        }

        [Then("only the following client filter methods should be executed:")]
        [Then("the client filter methods should be executed in the following order:")]
        public void ThenTheClientFilterMethodsWereExecuted(Table table)
        {
            Assert.AreEqual(table.RowCount, _clientFilterResults.Count);

            for (var i = 0; i < table.RowCount; i++)
            {
                var method = table.Rows[i]["Method"];
                Assert.AreEqual(method, _clientFilterResults[i]);
            }
        }

        [Then("the client exception filter should be executed")]
        public void ThenTheClientFilterWasExecuted()
        {
            Assert.AreNotEqual(0, _exceptionFilterResults.Count);
        }

        [Then("the following client exception filters should be executed:")]
        [Then("the client exception filters should be executed in the following order:")]
        public void ThenTheClientExceptionFiltersWereExecuted(Table table)
        {
            Assert.AreEqual(table.RowCount, _exceptionFilterResults.Count);

            for (var i = 0; i < table.RowCount; i++)
            {
                var filter = table.Rows[i]["Filter"];
                Assert.AreEqual(filter, _exceptionFilterResults[i]);
            }
        }

        [Then("an exception should not be thrown by the client")]
        public void ThenNoExceptionWereThrown()
        {
            Assert.IsNull(_exception);
        }

        [Then(@"it should have all of the above parameters encoded as JSON string")]
        public void ThenItShouldHaveAllOfTheAboveParametersEncodedAsJsonString()
        {
            var job = Redis.Client.GetAllEntriesFromHash(String.Format("hangfire:job:{0}", JobSteps.DefaultJobId));
            foreach (var parameter in _parameters)
            {
                Assert.IsTrue(job.ContainsKey(parameter.Key));
                Assert.AreEqual(parameter.Value, JobHelper.FromJson<string>(job[parameter.Key]));
            }
        }

        [Then(@"the '(\w+)' client filter got the actual values of the parameters")]
        public void ThenTheClientFilterGotTheActualValuesOfTheParameters(string name)
        {
            // Assert methods are called in the TestFilter class
        }
    }
}
