using CronExpressionDescriptor;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class CronExpressionDescriptorFacts
    {
        [Fact]
        public void ShouldNotThrowIfHasSeconds()
        {
            Assert.DoesNotThrow(() => ExpressionDescriptor.GetDescription(Cron.Secondly()));
        }


        [Fact]
        public void ParsesEverySecond()
        {
            string expected = "Every second";
            var actual = ExpressionDescriptor.GetDescription(Cron.Secondly());

            Assert.Equal(expected, actual);
        }
 
        [Fact]
        public void ParsesEveryFifteenSeconds()
        {
            string expected = "Every 15 seconds";
            var actual = ExpressionDescriptor.GetDescription("/15 * * * * *");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParsesComplexWithSeconds()
        {
            string expected = "Every 45 seconds, at 03 minutes past the hour, every 02 hours, only on Sunday and Saturday";
            var actual = ExpressionDescriptor.GetDescription("/45 3 /2 * * 0,6");

            Assert.Equal(expected, actual);
        }
 
    }
}