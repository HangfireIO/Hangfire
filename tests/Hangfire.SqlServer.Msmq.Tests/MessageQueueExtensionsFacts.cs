using MQTools;
using Xunit;

namespace Hangfire.SqlServer.Msmq.Tests
{
    public class MessageQueueExtensionsFacts
    {
        [Theory]
        [InlineData(@"ComputerName\Private$\QueueName", "ComputerName", "Private$", "QueueName")]
        [InlineData(@"ComputerName\QueueName", "ComputerName", "", "QueueName")]
        [InlineData(@".\Private$\QueueName", ".", "Private$", "QueueName")]
        [InlineData(@".\QueueName", ".", "", "QueueName")]
        [InlineData(@"FormatName:Direct=OS:ComputerName\QueueName", "ComputerName", "", "QueueName")]
        [InlineData(@"FormatName:Direct=OS:ComputerName\Private$\QueueName", "ComputerName", "Private$", "QueueName")]
        public void QueueRegex_CorrectlyParsesPublicAndPrivateQueuePaths(
            string queuePath, 
            string expectedComputerName,
            string expectedQueueType,
            string expectedQueueName)
        {
            var match = MessageQueueExtensions.GetQueuePathMatch(queuePath);

            Assert.NotNull(match);
            Assert.Equal(expectedComputerName, match.Groups["computerName"].Value);
            Assert.Equal(expectedQueueType, match.Groups["queueType"].Value);
            Assert.Equal(expectedQueueName, match.Groups["queue"].Value);
        }
    }
}
