using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class DataCompatibilityRangeTheoryTestCase : XunitTestCase
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        [UsedImplicitly]
        public DataCompatibilityRangeTheoryTestCase()
        {
        }

        public DataCompatibilityRangeTheoryTestCase(
            CompatibilityLevel compatibilityLevel,
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, new object[] { compatibilityLevel })
        {
        }

        public override Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            return new DataCompatibilityRangeTheoryTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();
        }
    }
}