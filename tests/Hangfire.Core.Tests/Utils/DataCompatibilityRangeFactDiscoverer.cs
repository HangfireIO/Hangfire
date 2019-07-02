using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Annotations;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    [UsedImplicitly]
    internal class DataCompatibilityRangeFactDiscoverer : IXunitTestCaseDiscoverer
    {
        public IMessageSink DiagnosticMessageSink { get; }

        public DataCompatibilityRangeFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            DiagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            if (testMethod.Method.GetParameters().Any())
            {
                yield return new ExecutionErrorTestCase(
                    DiagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    testMethod,
                    "[CompatibilityLevelFact] methods are not allowed to have parameters. Did you mean to use [CompatibilityLevelTheory]?");
            }
            else if (testMethod.Method.IsGenericMethodDefinition)
            {
                yield return new ExecutionErrorTestCase(
                    DiagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    testMethod, 
                    "[CompatibilityLevelFact] methods are not allowed to be generic.");
            }
            else
            {
                var compatibilityLevels = GetAllowedCompatibilityLevels(factAttribute);

                foreach (var compatibilityLevel in compatibilityLevels)
                {
                    yield return new DataCompatibilityRangeTestCase(
                        DiagnosticMessageSink,
                        discoveryOptions.MethodDisplayOrDefault(),
                        testMethod,
                        new object[] { compatibilityLevel });
                }
            }
        }

        internal static CompatibilityLevel[] GetAllowedCompatibilityLevels(IAttributeInfo attributeInfo)
        {
            var compatibilityLevels = Enum.GetValues(typeof(CompatibilityLevel))
                .Cast<CompatibilityLevel>()
                .ToArray();

            var minLevel = attributeInfo.GetNamedArgument<CompatibilityLevel>("MinLevel");
            var maxExcludingLevel = attributeInfo.GetNamedArgument<CompatibilityLevel>("MaxExcludingLevel");

            var result = new List<CompatibilityLevel>();

            foreach (var compatibilityLevel in compatibilityLevels)
            {
                if (compatibilityLevel >= minLevel && compatibilityLevel < maxExcludingLevel)
                {
                    result.Add(compatibilityLevel);
                }
            }

            return result.ToArray();
        }
    }
}