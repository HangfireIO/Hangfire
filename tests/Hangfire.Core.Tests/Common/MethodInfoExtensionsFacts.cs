using System.Linq;
using System.Reflection;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class MethodInfoExtensionsFacts
    {
        [Fact]
        public void GetNormalizedName_ReturnsNormalizedName_ForRegularMethod()
        {
            var service = new RegularInterfaceImplementation();
            var normalizedName = service.GetType().GetRuntimeMethods().First().GetNormalizedName();

            Assert.Equal("Method", normalizedName);
        }

        [Fact]
        public void GetNormalizedName_ReturnsNormalizedName_ForExplicitlyImplementedMethod()
        {
            var service = new ExplicitInterfaceImplementation();
            var normalizedName = service.GetType().GetRuntimeMethods().First().GetNormalizedName();

            Assert.Equal("Method", normalizedName);
        }

        private interface IService
        {
            void Method();
        }

        private class RegularInterfaceImplementation : IService
        {
            public void Method()
            {
            }
        }

        private class ExplicitInterfaceImplementation : IService
        {
            void IService.Method()
            {
            }
        }
    }
}
