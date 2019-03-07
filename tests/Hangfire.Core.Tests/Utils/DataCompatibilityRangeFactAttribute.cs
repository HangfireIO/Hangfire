using System;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    [XunitTestCaseDiscoverer("Hangfire.Core.Tests.DataCompatibilityRangeFactDiscoverer", "Hangfire.Core.Tests")]
    [AttributeUsage(AttributeTargets.Method)]
    internal class DataCompatibilityRangeFactAttribute : FactAttribute
    {
        internal static readonly CompatibilityLevel PossibleMinLevel;
        internal static readonly CompatibilityLevel PossibleMaxExcludingLevel;

        static DataCompatibilityRangeFactAttribute()
        {
            var compatibilityLevels = Enum.GetValues(typeof(CompatibilityLevel))
                .Cast<CompatibilityLevel>()
                .ToArray();

            PossibleMinLevel = compatibilityLevels.Min();
            PossibleMaxExcludingLevel = compatibilityLevels.Max() + 1;
        }

        public DataCompatibilityRangeFactAttribute()
        {
            MinLevel = PossibleMinLevel;
            MaxExcludingLevel = PossibleMaxExcludingLevel;
        }

        public CompatibilityLevel MinLevel { get; set; }
        public CompatibilityLevel MaxExcludingLevel { get; set; }
    }
}