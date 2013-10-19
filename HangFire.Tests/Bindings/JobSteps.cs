using System;
using System.Collections.Generic;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobSteps : Steps
    {
        public const string DefaultJobId = "some-id";
        private static readonly Type DefaultJobType = typeof(TestJob);

        [Given(@"a job")]
        public void GivenAJob()
        {
            Given(String.Format("a job of the '{0}' type", DefaultJobType.AssemblyQualifiedName));
        }

        [Given(@"the '(.+)' job")]
        public void GivenTheJob(string jobId)
        {
            Given(String.Format("the '{0}' job of the '{1}' type", jobId, DefaultJobType));
        }

        [Given(@"a job of the '(.+)' type")]
        public void GivenAJobOfTheType(string type)
        {
            Given(String.Format("the '{0}' job of the '{1}' type", DefaultJobId, type));
        }

        [Given(@"the '(.+)' job of the '(.+)' type")]
        public void GivenTheJobOfTheType(string jobId, string type)
        {
            Redis.Client.AddItemToList(
                String.Format("hangfire:job:{0}:history", jobId),
                "");

            Redis.Client.SetEntryInHash(
                String.Format("hangfire:job:{0}:state", jobId),
                "StateProp",
                "SomeValue");

            Redis.Client.SetRangeInHash(
                String.Format("hangfire:job:{0}", jobId),
                new Dictionary<string, string>
                    {
                        { "Type", type },
                        { "Args", JobHelper.ToJson(new Dictionary<string, string>()) },
                        { "State", EnqueuedState.Name },
                    });
        }

        [Given(@"it's state is (.+)")]
        public void GivenItsStateIs(string state)
        {
            Redis.Client.SetEntryInHash(
                String.Format("hangfire:job:{0}", DefaultJobId),
                "State",
                state);
        }

        [Then(@"the job moved to the (.+) state")]
        [Then(@"the job remains to be in the (.+) state")]
        [Then(@"the job becomes (.+)")]
        public void ThenTheJobMovedToTheState(string state)
        {
            var jobState = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", DefaultJobId),
                "State");

            Assert.AreEqual(state, jobState);
        }
    }
}