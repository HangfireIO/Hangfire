using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Storage
{
    public class StorageConnectionExtensionsFacts
    {
        private readonly Mock<IStorageConnection> _connection;

        public StorageConnectionExtensionsFacts()
        {
            _connection = new Mock<IStorageConnection>();
        }

        [Fact]
        public void GivenRecurringJobIsCancelledWhenGetRecurringJobsThenNotGetStateData()
        {
            _connection.Setup(o => o.GetAllItemsFromSet(It.IsAny<string>())).Returns(new HashSet<string> { "1" });
            _connection.Setup(o => o.GetAllEntriesFromHash("recurring-job:1")).Returns(new Dictionary<string, string>
                {
                    { "Cron", "A"},
                    { "Job", @"{""Type"":""ConsoleApplication1.CommandHandler, ConsoleApplication1"",""Method"":""Handle"",""ParameterTypes"":""[\""string\""]"",""Arguments"":""[\""Text\""]""}"},
                    { "LastJobId", string.Empty}
                }).Verifiable();

            var result = _connection.Object.GetRecurringJobs().Single();

            Assert.Null(result.LastJobState);
            _connection.VerifyAll();
        }

        [Fact]
        public void AcquireDistributedJobLock_AcquiresALock_WithTheCorrectResource()
        {
            var timeout = TimeSpan.FromSeconds(5);

            _connection.Object.AcquireDistributedJobLock("some-id", timeout);

            _connection.Verify(x => x.AcquireDistributedLock(
                "job:some-id:state-lock",
                timeout));
        }

	    [Fact]
	    public void GetRecurringJobsWithNullDateTimeHashValues() 
		{
			_connection.Setup(o => o.GetAllItemsFromSet(It.IsAny<string>())).Returns(new HashSet<string> { "1" });
		    _connection.Setup(o => o.GetAllEntriesFromHash("recurring-job:1")).Returns(new Dictionary<string, string>
		    {
			    { "Cron", "A"},
			    { "Job", @"{""Type"":""ConsoleApplication1.CommandHandler, ConsoleApplication1"",""Method"":""Handle"",""ParameterTypes"":""[\""string\""]"",""Arguments"":""[\""Text\""]""}"},
				{ "NextExecution", null },
			    { "LastExecution", null },
			    { "CreatedAt", null }
		    }).Verifiable();

			var result = _connection.Object.GetRecurringJobs();
			Assert.Null(result[0].LastExecution);
			Assert.Null(result[0].NextExecution);
			Assert.Null(result[0].CreatedAt);
			_connection.VerifyAll();
		}

	}
}
