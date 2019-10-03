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
    public class DataCompatibilityRangeTheoryTestCaseRunner : XunitTestCaseRunner
    {
        private readonly ExceptionAggregator _cleanupAggregator = new ExceptionAggregator();
        private Exception _dataDiscoveryException;
        private readonly IMessageSink _diagnosticMessageSink;
        private readonly List<XunitTestRunner> _testRunners = new List<XunitTestRunner>();
        private readonly List<IDisposable> _toDispose = new List<IDisposable>();

        public DataCompatibilityRangeTheoryTestCaseRunner(
             IXunitTestCase testCase,
             string displayName,
             string skipReason,
             object[] constructorArguments,
             object[] testMethodArguments,
             IMessageSink diagnosticMessageSink,
             IMessageBus messageBus,
             ExceptionAggregator aggregator,
             CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task AfterTestCaseStartingAsync()
        {
            await base.AfterTestCaseStartingAsync();

            try
            {
                var dataAttributes = TestCase.TestMethod.Method.GetCustomAttributes(typeof(DataAttribute));

                foreach (var dataAttribute in dataAttributes)
                {
                    var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                    var args = discovererAttribute.GetConstructorArguments().Cast<string>().ToList();
                    var discovererType = Type.GetType($"{args[0]}, {args[1]}");
                    if (discovererType == null)
                    {
                        if (dataAttribute is IReflectionAttributeInfo reflectionAttribute)
                            Aggregator.Add(new InvalidOperationException($"Data discoverer specified for {reflectionAttribute.Attribute.GetType()} on {TestCase.TestMethod.TestClass.Class.Name}.{TestCase.TestMethod.Method.Name} does not exist."));
                        else
                            Aggregator.Add(new InvalidOperationException($"A data discoverer specified on {TestCase.TestMethod.TestClass.Class.Name}.{TestCase.TestMethod.Method.Name} does not exist."));

                        continue;
                    }

                    IDataDiscoverer discoverer;
                    try
                    {
                        discoverer = ExtensibilityPointFactory.GetDataDiscoverer(_diagnosticMessageSink, discovererType);
                    }
                    catch (InvalidCastException)
                    {
                        Aggregator.Add(dataAttribute is IReflectionAttributeInfo reflectionAttribute
                            ? new InvalidOperationException(
                                $"Data discoverer specified for {reflectionAttribute.Attribute.GetType()} on {TestCase.TestMethod.TestClass.Class.Name}.{TestCase.TestMethod.Method.Name} does not implement IDataDiscoverer.")
                            : new InvalidOperationException(
                                $"A data discoverer specified on {TestCase.TestMethod.TestClass.Class.Name}.{TestCase.TestMethod.Method.Name} does not implement IDataDiscoverer."));

                        continue;
                    }

                    var data = discoverer.GetData(dataAttribute, TestCase.TestMethod.Method);
                    if (data == null)
                    {
                        Aggregator.Add(new InvalidOperationException($"Test data returned null for {TestCase.TestMethod.TestClass.Class.Name}.{TestCase.TestMethod.Method.Name}. Make sure it is statically initialized before this test method is called."));
                        continue;
                    }

                    foreach (var dataRow in data)
                    {
                        _toDispose.AddRange(dataRow.OfType<IDisposable>());

                        ITypeInfo[] resolvedTypes = null;
                        var methodToRun = TestMethod;
                        var convertedDataRow = methodToRun.ResolveMethodArguments(dataRow);

                        if (methodToRun.IsGenericMethodDefinition)
                        {
                            resolvedTypes = TestCase.TestMethod.Method.ResolveGenericTypes(convertedDataRow);
                            methodToRun = methodToRun.MakeGenericMethod(resolvedTypes.Select(t => ((IReflectionTypeInfo)t).Type).ToArray());
                        }

                        var parameterTypes = methodToRun.GetParameters().Select(p => p.ParameterType).ToArray();
                        convertedDataRow = Reflector.ConvertArguments(convertedDataRow, parameterTypes);

                        object compatibilityLevel;

                        if (TestMethodArguments[0] is CompatibilityLevel level)
                        {
                            compatibilityLevel = level;
                        }
                        else
                        {
                            compatibilityLevel = Enum.Parse(typeof(CompatibilityLevel), (string)TestMethodArguments[0]);
                        }

                        var finalDataRow = convertedDataRow.Concat(new [] { compatibilityLevel }).ToArray();

                        var theoryDisplayName = TestCase.TestMethod.Method.GetDisplayNameWithArguments(DisplayName, finalDataRow, resolvedTypes);
                        var test = new XunitTest(TestCase, theoryDisplayName);
                        var skipReason = SkipReason ?? dataAttribute.GetNamedArgument<string>("Skip");

                        var testRunner = new DataCompatibilityRangeTestRunner(test, MessageBus, TestClass, ConstructorArguments,
                            methodToRun, finalDataRow, skipReason, BeforeAfterAttributes, Aggregator,
                            CancellationTokenSource);

                        _testRunners.Add(testRunner);
                    }
                }
            }
            catch (Exception ex)
            {
                _dataDiscoveryException = ex;
            }
        }

        protected override Task BeforeTestCaseFinishedAsync()
        {
            Aggregator.Aggregate(_cleanupAggregator);

            return base.BeforeTestCaseFinishedAsync();
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            if (_dataDiscoveryException != null)
                return RunTest_DataDiscoveryException();

            var runSummary = new RunSummary();
            foreach (var testRunner in _testRunners)
                runSummary.Aggregate(await testRunner.RunAsync());

            var timer = new ExecutionTimer();
            foreach (var disposable in _toDispose)
                timer.Aggregate(() => _cleanupAggregator.Run(disposable.Dispose));

            runSummary.Time += timer.Total;
            return runSummary;
        }

        private RunSummary RunTest_DataDiscoveryException()
        {
            var test = new XunitTest(TestCase, DisplayName);

            if (!MessageBus.QueueMessage(new TestStarting(test)))
                CancellationTokenSource.Cancel();
            else if (!MessageBus.QueueMessage(new TestFailed(test, 0, null, Unwrap(_dataDiscoveryException))))
                CancellationTokenSource.Cancel();
            if (!MessageBus.QueueMessage(new TestFinished(test, 0, null)))
                CancellationTokenSource.Cancel();

            return new RunSummary { Total = 1, Failed = 1 };
        }

        private static Exception Unwrap(Exception ex)
        {
            while (true)
            {
                if (ex is TargetInvocationException invocationException)
                    ex = invocationException.InnerException;
                else
                    break;
            }
            return ex;
        }
    }
}
