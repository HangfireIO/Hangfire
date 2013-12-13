using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Server;
using HangFire.Server.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class ServerWatchdogSteps : Steps
    {
        private ServerWatchdog _watchdog;

        [Given(@"a server watchdog")]
        public void GivenAServerWatchdog()
        {
            _watchdog = new ServerWatchdog(RedisFactory.BasicManager);
        }

        [Given(@"a server that was started (.+)")]
        public void GivenAServerThatWasStarted(DateTime startedAt)
        {
            GivenAServerThatWasStarted(ServerSteps.DefaultServerName, startedAt);
        }

        [Given(@"a server '(\w+)' that was started (.+)")]
        public void GivenAServerThatWasStarted(string name, DateTime startedAt)
        {
            Redis.Client.AddItemToSet("hangfire:servers", name);
            Redis.Client.SetEntryInHash(
                String.Format("hangfire:server:{0}", name),
                "StartedAt",
                JobHelper.ToStringTimestamp(startedAt));
        }

        [Given(@"its last heartbeat was (.+)")]
        public void GivenItsLastHeartbeatWas(DateTime lastHeartbeat)
        {
            Redis.Client.SetEntryInHash(
                String.Format("hangfire:server:{0}", ServerSteps.DefaultServerName),
                "Heartbeat",
                JobHelper.ToStringTimestamp(lastHeartbeat));
        }

        [Given(@"there are no any heartbeats")]
        public void GivenThereAreNoAnyHeartbeats()
        {
        }

        [When(@"the watchdog gets the job done")]
        public void WhenTheWatchdogGetsTheJobDone()
        {
            _watchdog.RemoveTimedOutServers();
        }

        [Then(@"the server should not be removed")]
        public void ThenTheServerShouldNotBeRemoved()
        {
            ThenTheServerShouldNotBeRemoved(ServerSteps.DefaultServerName);
        }

        [Then(@"the server '(\w+)' should not be removed")]
        public void ThenTheServerShouldNotBeRemoved(string name)
        {
            Assert.IsTrue(Redis.Client.SetContainsItem("hangfire:servers", name));
            Assert.IsTrue(
                Redis.Client.ContainsKey(String.Format("hangfire:server:{0}", name)));
        }

        [Then(@"the server should be removed")]
        public void ThenTheServerShouldBeRemoved()
        {
            ThenTheServerShouldBeRemoved(ServerSteps.DefaultServerName);
        }

        [Then(@"the server '(\w+)' should be removed")]
        public void ThenTheServerShouldBeRemoved(string name)
        {
            Assert.IsFalse(Redis.Client.SetContainsItem("hangfire:servers", name));
            Assert.IsFalse(
                Redis.Client.ContainsKey(String.Format("hangfire:server:{0}", name)));
        }
    }
}
