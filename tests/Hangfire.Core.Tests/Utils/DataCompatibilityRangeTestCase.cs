using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class DataCompatibilityRangeTestCase : XunitTestCase
    {
#pragma warning disable 618
        public DataCompatibilityRangeTestCase()
#pragma warning restore 618
        {
        }

        public DataCompatibilityRangeTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            ITestMethod testMethod,
            object[] testMethodArguments)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
        }

        public override Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink, 
            IMessageBus messageBus, 
            object[] constructorArguments,
            ExceptionAggregator aggregator, 
            CancellationTokenSource cancellationTokenSource)
        {
            return new DataCompatibilityRangeTestCaseRunner(
                    this,
                    DisplayName,
                    SkipReason,
                    constructorArguments,
                    TestMethodArguments,
                    messageBus,
                    aggregator,
                    cancellationTokenSource)
                .RunAsync();
        }
    }
}