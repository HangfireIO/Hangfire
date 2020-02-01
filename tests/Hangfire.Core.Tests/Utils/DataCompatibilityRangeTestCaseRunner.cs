using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class DataCompatibilityRangeTestCaseRunner : XunitTestCaseRunner
    {
        public DataCompatibilityRangeTestCaseRunner(
            IXunitTestCase testCase,
            string displayName,
            string skipReason,
            object[] constructorArguments,
            object[] testMethodArguments,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator,
                cancellationTokenSource)
        {
        }

        protected override Task<RunSummary> RunTestAsync()
        {
            return new DataCompatibilityRangeTestRunner(new XunitTest(TestCase, DisplayName), MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, SkipReason, BeforeAfterAttributes, new ExceptionAggregator(Aggregator), CancellationTokenSource)
                .RunAsync();
        }
    }
}