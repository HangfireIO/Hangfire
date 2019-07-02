using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class DataCompatibilityRangeTestRunner : XunitTestRunner
    {
        private static readonly SemaphoreSlim SyncRoot = new SemaphoreSlim(1, 1);

        public DataCompatibilityRangeTestRunner(
            ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override async Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
        {
            CompatibilityLevel? oldCompatibilityLevel = null;

            try
            {
                await SyncRoot.WaitAsync(CancellationTokenSource.Token);

                var compatibilityLevel = (CompatibilityLevel)TestMethodArguments[TestMethodArguments.Length - 1];
                TestMethodArguments = TestMethodArguments.Take(TestMethodArguments.Length - 1).ToArray();

                oldCompatibilityLevel = GlobalConfiguration.CompatibilityLevel;
                GlobalConfiguration.CompatibilityLevel = compatibilityLevel;

                return await base.InvokeTestAsync(aggregator);
            }
            finally
            {
                if (oldCompatibilityLevel.HasValue) GlobalConfiguration.CompatibilityLevel = oldCompatibilityLevel.Value;
                SyncRoot.Release();
            }
        }
    }
}