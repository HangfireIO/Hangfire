using System.Collections.Generic;
using System.Threading;
using HangFire.Filters;
using HangFire.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobManagerSteps : Steps
    {
        private readonly IList<IServerFilter> _serverFilters = new List<IServerFilter>();
        private readonly IList<IServerExceptionFilter> _exceptionFilters = new List<IServerExceptionFilter>();

        private readonly IList<string> _serverResults = new List<string>();
        private readonly IList<string> _exceptionResults = new List<string>();
    
        [BeforeScenario]
        public void BeforeScenario()
        {
            TestJob.Performed = false;
            TestJob.Disposed = false;
        }

        [Given(@"a manager")]
        public void GivenAManager()
        {
            
        }

        [Given(@"a server filter '(\w+)'")]
        public void GivenAServerFilter(string name)
        {
            _serverFilters.Add(new TestFilter(name, _serverResults));
        }

        [Given(@"a server filter '(\w+)' that cancels the performing")]
        public void GivenAServerFilterThatCancelsThePerforming(string name)
        {
            _serverFilters.Add(new TestFilter(name, _serverResults, false, true));
        }

        [Given(@"a server filter '(\w+)' that throws an exception")]
        public void GivenAServerFilterThatThrowsAnException(string name)
        {
            _serverFilters.Add(new TestFilter(name, _serverResults, true));
        }

        [Given(@"a server filter '(\w+)' that handles an exception")]
        public void GivenAServerFilterThatHandlesAnException(string name)
        {
            _serverFilters.Add(new TestFilter(name, _serverResults, false, false, true));
        }

        [Given(@"a server exception filter '(\w+)'")]
        public void GivenAServerExceptionFilter(string name)
        {
            _exceptionFilters.Add(new TestExceptionFilter(name, _exceptionResults));
        }

        [Given(@"a server exception filter '(\w+)' that handles an exception")]
        public void GivenAServerExceptionFilterThatHandlesAnException(string name)
        {
            _exceptionFilters.Add(new TestExceptionFilter(name, _exceptionResults, true));
        }

        [When(@"the manager processes the next job")]
        public void WhenTheWorkerPerformsTheJob()
        {
            var context = new ServerContext(
                ServerSteps.DefaultServerName,
                new JobActivator(),
                new JobPerformer(_serverFilters, _exceptionFilters));

            using (var manager = new JobManager(
                new JobFetcher(RedisFactory.BasicManager, QueueSteps.DefaultQueue),
                RedisFactory.BasicManager,
                context,
                1))
            {
                manager.ProcessNextJob(new CancellationTokenSource().Token);
            }
        }

        [Then(@"the job should be performed")]
        public void ThenTheJobShouldBePerformed()
        {
            Assert.IsTrue(TestJob.Performed);
        }

        [Then(@"the job should not be performed")]
        public void ThenTheJobShouldNotBePerformed()
        {
            Assert.IsFalse(TestJob.Performed);
        }

        [Then(@"there should be no performing actions")]
        public void ThenThereShouldBeNoPerformingActions()
        {
            // TODO: what is this?
        }

        [Then(@"the job should be disposed")]
        public void ThenTheJobShouldBeDisposed()
        {
            Assert.IsTrue(TestJob.Disposed);
        }

        [Then(@"only the following server filter methods should be executed:")]
        [Then(@"the server filter methods should be executed in the following order:")]
        public void ThenTheServerFilterMethodsShouldBeExecutedInTheFollowingOrder(Table table)
        {
            Assert.AreEqual(table.RowCount, _serverResults.Count);

            for (var i = 0; i < table.RowCount; i++)
            {
                var method = table.Rows[i]["Method"];
                Assert.AreEqual(method, _serverResults[i]);
            }
        }

        [Then("the server exception filter should be executed")]
        public void ThenTheClientFilterWasExecuted()
        {
            Assert.AreNotEqual(0, _exceptionResults.Count);
        }

        [Then("the following server exception filters should be executed:")]
        [Then("the server exception filters should be executed in the following order:")]
        public void ThenTheClientExceptionFiltersWereExecuted(Table table)
        {
            Assert.AreEqual(table.RowCount, _exceptionResults.Count);

            for (var i = 0; i < table.RowCount; i++)
            {
                var filter = table.Rows[i]["Filter"];
                Assert.AreEqual(filter, _exceptionResults[i]);
            }
        }
    }
}
