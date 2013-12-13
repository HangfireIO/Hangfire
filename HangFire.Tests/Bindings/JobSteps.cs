using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobSteps : Steps
    {
        public const string DefaultJobId = "some_id";
        public static readonly Type DefaultJobType = typeof(TestJob);

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
            GivenTheJobOfTheTypeWithTheFollowingArguments(jobId, type, new Table("Name", "Value"));
        }

        [Given(@"a job of the '(\w+)' type with the following arguments:")]
        public void GivenAJobOfTheTypeWithTheFollowingArguments(string type, Table args)
        {
            GivenTheJobOfTheTypeWithTheFollowingArguments(JobSteps.DefaultJobId, type, args);
        }

        [Given(@"the '(.+)' job of the '(.+)' type with the following arguments:")]
        public void GivenTheJobOfTheTypeWithTheFollowingArguments(string jobId, string type, Table args)
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
                        { "Args", JobHelper.ToJson(args.Rows.ToDictionary(x => x["Name"], x => x["Value"])) },
                        { "State", EnqueuedState.Name },
                    });
        }

        [Given(@"an enqueued CustomJob with the following arguments:")]
        public void GivenAnEnqueuedCustomJobWithTheFollowingArguments(Table table)
        {
            GivenAJobOfTheTypeWithTheFollowingArguments(typeof(CustomJob).AssemblyQualifiedName, table);
            Redis.Client.EnqueueItemOnList(
                String.Format("hangfire:queue:{0}", QueueSteps.DefaultQueue),
                DefaultJobId);
        }

        [Given(@"a job with empty state")]
        public void GivenAJobWithEmptyState()
        {
            Redis.Client.SetRangeInHash(
                String.Format("hangfire:job:{0}", DefaultJobId),
                new Dictionary<string, string>
                    {
                        { "Type", typeof(TestJob).AssemblyQualifiedName },
                        { "Args", JobHelper.ToJson(new Dictionary<string, string>()) },
                        { "State", String.Empty }
                    });
        }

        [Given(@"its state is (.+)")]
        public void GivenItsStateIs(string state)
        {
            Redis.Client.SetEntryInHash(
                String.Format("hangfire:job:{0}", DefaultJobId),
                "State",
                state);
        }

        [Then(@"the state of the job should be (\w+)")]
        [Then(@"its state should be (\w+)")]
        [Then(@"the job should be moved to the (.+) state")]
        [Then(@"the job should be in the (\w+) state")]
        public void ThenTheJobMovedToTheState(string state)
        {
            var jobState = Redis.Client.GetValueFromHash(
                String.Format("hangfire:job:{0}", DefaultJobId),
                "State");

            Assert.AreEqual(state, jobState);
        }
    }
}