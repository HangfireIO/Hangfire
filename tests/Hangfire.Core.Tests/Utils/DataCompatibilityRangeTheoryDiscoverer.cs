using System;
using System.Collections.Generic;
using System.Linq;
#if NETCOREAPP1_0
using System.Reflection;
#endif
using Hangfire.Annotations;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    [UsedImplicitly]
    internal class DataCompatibilityRangeTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        public DataCompatibilityRangeTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            DiagnosticMessageSink = diagnosticMessageSink;
        }

        protected IMessageSink DiagnosticMessageSink { get; }

        protected virtual IEnumerable<IXunitTestCase> CreateTestCasesForTheory(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute)
        {
            var compatibilityLevels = DataCompatibilityRangeFactDiscoverer.GetAllowedCompatibilityLevels(theoryAttribute);

            foreach (var compatibilityLevel in compatibilityLevels)
            {
                yield return new DataCompatibilityRangeTheoryTestCase(
                    compatibilityLevel,
                    DiagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    testMethod);
            }
        }

        protected virtual IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute,
            object[] dataRow)
        {
            var compatibilityLevels = DataCompatibilityRangeFactDiscoverer.GetAllowedCompatibilityLevels(theoryAttribute);

            foreach (var compatibilityLevel in compatibilityLevels)
            {
                yield return new DataCompatibilityRangeTestCase(
                    DiagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    testMethod,
                    dataRow.Concat(new object[] { compatibilityLevel }).ToArray());
            }
        }

        protected virtual IEnumerable<IXunitTestCase> CreateTestCasesForSkip(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute,
            string skipReason)
        {
            return new[]
            {
                new XunitTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod)
            };
        }

        protected virtual IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute,
            object[] dataRow,
            string skipReason)
        {
            return new[]
            {
                new XunitSkippedDataRowTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, skipReason, dataRow)
            };
        }

        public virtual IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute)
        {
            var skipArgument = theoryAttribute.GetNamedArgument<string>("Skip");
            if (skipArgument != null)
            {
                return CreateTestCasesForSkip(discoveryOptions, testMethod, theoryAttribute, skipArgument);
            }

            if (discoveryOptions.PreEnumerateTheoriesOrDefault())
            {
                try
                {
                    var customAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute));
                    var testCases = new List<IXunitTestCase>();

                    foreach (var attributeInfo in customAttributes)
                    {
                        var dataDiscovererAttribute = attributeInfo.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                        IDataDiscoverer dataDiscoverer;
                        try
                        {
                            dataDiscoverer = ExtensibilityPointFactory.GetDataDiscoverer(DiagnosticMessageSink, dataDiscovererAttribute);
                        }
                        catch (InvalidCastException)
                        {
                            if (attributeInfo is IReflectionAttributeInfo reflectionAttributeInfo)
                            {
                                testCases.Add(new ExecutionErrorTestCase(
                                    DiagnosticMessageSink,
                                    discoveryOptions.MethodDisplayOrDefault(),
                                    testMethod,
                                    $"Data discoverer specified for {reflectionAttributeInfo.Attribute.GetType()} on {testMethod.TestClass.Class.Name}.{testMethod.Method.Name} does not implement IDataDiscoverer."));

                                continue;
                            }

                            testCases.Add(new ExecutionErrorTestCase(
                                DiagnosticMessageSink,
                                discoveryOptions.MethodDisplayOrDefault(),
                                testMethod,
                                $"A data discoverer specified on {testMethod.TestClass.Class.Name}.{testMethod.Method.Name} does not implement IDataDiscoverer."));

                            continue;
                        }
                        if (dataDiscoverer == null)
                        {
                            if (attributeInfo is IReflectionAttributeInfo reflectionAttributeInfo)
                            {
                                testCases.Add(new ExecutionErrorTestCase(
                                    DiagnosticMessageSink,
                                    discoveryOptions.MethodDisplayOrDefault(),
                                    testMethod,
                                    $"Data discoverer specified for {reflectionAttributeInfo.Attribute.GetType()} on {testMethod.TestClass.Class.Name}.{testMethod.Method.Name} does not exist."));
                            }
                            else
                            {
                                testCases.Add(new ExecutionErrorTestCase(
                                    DiagnosticMessageSink,
                                    discoveryOptions.MethodDisplayOrDefault(),
                                    testMethod,
                                    $"A data discoverer specified on {testMethod.TestClass.Class.Name}.{testMethod.Method.Name} does not exist."));
                            }
                        }
                        else
                        {
                            if (!dataDiscoverer.SupportsDiscoveryEnumeration(attributeInfo, testMethod.Method))
                            {
                                testCases.Add(new ExecutionErrorTestCase(
                                    DiagnosticMessageSink,
                                    discoveryOptions.MethodDisplayOrDefault(),
                                    testMethod,
                                    $"DataDiscoverer doesn't support discovery enumeration for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}."));
                            }

                            var data = dataDiscoverer.GetData(attributeInfo, testMethod.Method);
                            if (data == null)
                            {
                                testCases.Add(new ExecutionErrorTestCase(
                                    DiagnosticMessageSink,
                                    discoveryOptions.MethodDisplayOrDefault(),
                                    testMethod,
                                    $"Test data returned null for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}. Make sure it is statically initialized before this test method is called."));
                            }
                            else
                            {
                                var serializationHelperType = Type.GetType("Xunit.Sdk.SerializationHelper, xunit.execution.desktop", throwOnError: false);
                                if (serializationHelperType == null)
                                {
                                    serializationHelperType = Type.GetType("Xunit.Sdk.SerializationHelper, xunit.execution.dotnet", throwOnError: false);

                                    if (serializationHelperType == null)
                                    {
                                        DiagnosticMessageSink.OnMessage(new DiagnosticMessage(
                                            $"Xunit.Sdk.SerializationHelper type not found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}; falling back to single test case."));

                                        return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
                                    }
                                }

                                var isSerializableMethod = serializationHelperType.GetMethod("IsSerializable", new [] { typeof(object) });
                                if (isSerializableMethod == null)
                                {
                                    DiagnosticMessageSink.OnMessage(new DiagnosticMessage(
                                        $"Xunit.Sdk.SerializationHelper.IsSerializable method not found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}; falling back to single test case."));

                                    return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
                                }

                                var skipArgument2 = attributeInfo.GetNamedArgument<string>("Skip");

                                foreach (var dataRow in data)
                                {
                                    if (!(bool)isSerializableMethod.Invoke(null, new object[] { dataRow }))
                                    {
                                        DiagnosticMessageSink.OnMessage(new DiagnosticMessage(
                                            $"Non-serializable data ('{dataRow.GetType().FullName}') found for '{testMethod.TestClass.Class.Name}.{testMethod.Method.Name}'; falling back to single test case."));

                                        return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
                                    }

                                    var collection = skipArgument2 != null
                                        ? CreateTestCasesForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, skipArgument2)
                                        : CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow);

                                    testCases.AddRange(collection);
                                }
                            }
                        }
                    }

                    if (testCases.Count == 0)
                    {
                        testCases.Add(new ExecutionErrorTestCase(
                            DiagnosticMessageSink,
                            discoveryOptions.MethodDisplayOrDefault(),
                            testMethod,
                            $"No data found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}"));
                    }

                    return testCases;
                }
                catch (Exception ex)
                {
                    DiagnosticMessageSink.OnMessage(new DiagnosticMessage(
                        $"Exception thrown during theory discovery on '{testMethod.TestClass.Class.Name}.{testMethod.Method.Name}'; falling back to single test case.{Environment.NewLine}{ex}"));
                }
            }

            return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        }
    }
}