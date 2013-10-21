using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Client;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceStack.Redis;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ClientSteps : Steps
    {
        private JobClient _client;
        private Mock<JobState> _stateMock;
        private IDictionary<string, string> _arguments = new Dictionary<string, string>();
        private Exception _exception;

        [Given("a client")]
        public void GivenAClient()
        {
            _client = new JobClient(RedisFactory.BasicManager);
        }

        [When("I create a job")]
        [When("I create an argumentless job")]
        public void WhenICreateAJob()
        {
            _stateMock = new Mock<JobState>("SomeReason");
            _stateMock.Setup(x => x.StateName).Returns("Test");
            _stateMock.Setup(x => x.GetProperties()).Returns(new Dictionary<string, string>());

            _client.CreateJob(
                JobSteps.DefaultJobId, 
                typeof (TestJob), 
                _stateMock.Object, 
                _arguments);
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
                _client.CreateJob(null, typeof (TestJob), new Mock<JobState>("1").Object, null);
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
                _client.CreateJob(JobSteps.DefaultJobId, null, new Mock<JobState>("1").Object, null);
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
                _client.CreateJob(JobSteps.DefaultJobId, typeof(TestJob), null, null);
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
                _client.CreateJob(JobSteps.DefaultJobId, typeof(ClientSteps), null, null);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [Then("the storage contains the job")]
        public void ThenTheStorageContainsIt()
        {
            Assert.IsTrue(Redis.Client.ContainsKey("hangfire:job:" + JobSteps.DefaultJobId));
        }

        [Then("it has the following parameters:")]
        public void ThenItHasTheFollowingParameters(Table table)
        {
            var job = Redis.Client.GetAllEntriesFromHash("hangfire:job:" + JobSteps.DefaultJobId);
            DictionaryAssert.ContainsFollowingItems(table, job);
        }

        [Then("the job contains all of the above arguments in the JSON format")]
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

        [Then("the given state was applied to it")]
        public void ThenTheGivenStateWasAppliedToIt()
        {
            _stateMock.Verify(
                x => x.Apply(It.IsAny<IRedisTransaction>(), JobSteps.DefaultJobId), 
                Times.Once);
        }

        [Then("A '(.+)' is thrown")]
        public void ThenAnExceptionIsThrown(string exceptionType)
        {
            Assert.IsNotNull(_exception);
            Assert.IsInstanceOfType(_exception, Type.GetType(exceptionType, true));
        }
    }
}
