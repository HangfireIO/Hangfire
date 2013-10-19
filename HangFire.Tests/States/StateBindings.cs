using System;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Common.Extensions;
using TechTalk.SpecFlow;

namespace HangFire.Tests.States
{
    [Binding]
    public class StateBindings
    {
        private JobState _state;

        [Given(@"the Succeeded state")]
        public void GivenTheSucceededState()
        {
            _state = new SucceededState(JobSteps.DefaultJobId, "Some reason");
        }

        [When(@"I apply it")]
        public void WhenIApplyIt()
        {
            using (var transaction = Redis.Client.CreateTransaction())
            {
                _state.Apply(transaction);
                transaction.Commit();
            }
        }

        [When(@"after I unapply it")]
        public void WhenAfterIUnapplyIt()
        {
            using (var transaction = Redis.Client.CreateTransaction())
            {
                var descriptor = JobState.GetDescriptor(_state.StateName);
                descriptor.Unapply(transaction, JobSteps.DefaultJobId);

                transaction.Commit();
            }
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
    }
}
