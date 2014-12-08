using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Storage
{
    public class StorageConnectionExtensionsFacts
    {
        [Fact]
        public void GivenRecurringJobIsCancelledWhenGetRecurringJobsThenNotGetStateData()
        {
            var connectionFake = new Mock<IStorageConnection>();
            connectionFake.Setup(o => o.GetAllItemsFromSet(It.IsAny<string>())).Returns(new HashSet<string> {"1"});
            connectionFake.Setup(o => o.GetAllEntriesFromHash("recurring-job:1")).Returns(new Dictionary<string, string>
                {
                    { "Cron", "A"},
                    { "Job", @"{""Type"":""ConsoleApplication1.CommandHandler, ConsoleApplication1"",""Method"":""Handle"",""ParameterTypes"":""[\""string\""]"",""Arguments"":""[\""Text\""]""}"},
                    { "LastJobId", string.Empty}
                }).Verifiable();

            var result = connectionFake.Object.GetRecurringJobs().Single();

            Assert.Null(result.LastJobState);
            connectionFake.VerifyAll();
        }
    }
}
