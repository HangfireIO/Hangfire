using System.Reflection;
using Xunit.Sdk;

namespace Hangfire.Core.Tests
{
    internal class CompatibilityLevelAttribute : BeforeAfterTestAttribute
    {
        private readonly CompatibilityLevel _compatibilityLevel;
        private CompatibilityLevel _oldCompatibilityLevel;

        public CompatibilityLevelAttribute(CompatibilityLevel compatibilityLevel)
        {
            _compatibilityLevel = compatibilityLevel;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            _oldCompatibilityLevel = GlobalConfiguration.CompatibilityLevel;
            GlobalConfiguration.CompatibilityLevel = _compatibilityLevel;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            GlobalConfiguration.CompatibilityLevel = _oldCompatibilityLevel;
        }
    }
}