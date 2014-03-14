using System;
using HangFire.Common;
using HangFire.Redis;
using HangFire.Server.Components;
using TechTalk.SpecFlow;
using Xunit;

namespace HangFire.Tests
{
    [Binding]
    public class ServerWatchdogSteps : Steps
    {
        private ServerWatchdog _watchdog;

        [Given(@"a server watchdog")]
        public void GivenAServerWatchdog()
        {
            _watchdog = new ServerWatchdog(Redis.Storage);
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
            _watchdog.RemoveTimedOutServers(TimeSpan.FromMinutes(1));
        }

        [Then(@"the server should not be removed")]
        public void ThenTheServerShouldNotBeRemoved()
        {
            ThenTheServerShouldNotBeRemoved(ServerSteps.DefaultServerName);
        }

        [Then(@"the server '(\w+)' should not be removed")]
        public void ThenTheServerShouldNotBeRemoved(string name)
        {
            Assert.True(Redis.Client.SetContainsItem("hangfire:servers", name));
            Assert.True(
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
            Assert.False(Redis.Client.SetContainsItem("hangfire:servers", name));
            Assert.False(
                Redis.Client.ContainsKey(String.Format("hangfire:server:{0}", name)));
        }
    }
}
