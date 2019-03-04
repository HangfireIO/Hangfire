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
        internal static readonly CompatibilityLevel PossibleMaxLevel;

        static DataCompatibilityRangeFactAttribute()
        {
            var compatibilityLevels = Enum.GetValues(typeof(CompatibilityLevel))
                .Cast<CompatibilityLevel>()
                .ToArray();

            PossibleMinLevel = compatibilityLevels.Min();
            PossibleMaxLevel = compatibilityLevels.Max();
        }

        public DataCompatibilityRangeFactAttribute()
        {
            MinLevel = PossibleMinLevel;
            MaxLevel = PossibleMaxLevel;
        }

        public CompatibilityLevel MinLevel { get; set; }
        public CompatibilityLevel MaxLevel { get; set; }
    }
}