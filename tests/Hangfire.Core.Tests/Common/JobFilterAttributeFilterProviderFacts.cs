using System.Linq;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobFilterAttributeFilterProviderFacts
    {
        [Fact]
        public void GetFilters_WithNullJob_ReturnsEmptyList()
        {
            // Arrange
            var provider = new JobFilterAttributeFilterProvider();

            // Act
            var result = provider.GetFilters(null);

            // Assert
            Assert.Empty(result);
        }

        [MyFilter(Order = 2112)]
// ReSharper disable once ClassNeverInstantiated.Local
        private class ClassWithTypeAttribute
        {
            public static void Method() { }
        }

        [Fact]
        public void GetFilters_IncludesAttributesOnClassType()
        {
            // Arrange
            var job = Job.FromExpression(() => ClassWithTypeAttribute.Method());
            var provider = new JobFilterAttributeFilterProvider();

            // Act
            var filter = provider.GetFilters(job).Single();

            // Assert
            var attribute = filter.Instance as MyFilterAttribute;
            Assert.NotNull(attribute);
            Assert.Equal(JobFilterScope.Type, filter.Scope);
            Assert.Equal(2112, filter.Order);
        }

// ReSharper disable once ClassNeverInstantiated.Local
        private class ClassWithActionAttribute
        {
            [MyFilter(Order = 1234)]
            public static void Method()
            {
            }
        }

        [Fact]
        public void GetFilters_IncludesAttributesOMethod()
        {
            // Arrange
            var job = Job.FromExpression(() => ClassWithActionAttribute.Method());
            var provider = new JobFilterAttributeFilterProvider();

            // Act
            var filter = provider.GetFilters(job).Single();

            // Assert
            var attribute = filter.Instance as MyFilterAttribute;
            Assert.NotNull(attribute);
            Assert.Equal(JobFilterScope.Method, filter.Scope);
            Assert.Equal(1234, filter.Order);
        }

        private abstract class BaseClass
        {
            public void MyMethod()
            {
            }
        }

        [MyFilter]
// ReSharper disable once ClassNeverInstantiated.Local
        private class DerivedClass : BaseClass
        {
        }

        [Fact]
        public void GetFilters_IncludesTypeAttributesFromDerivedTypeWhenMethodIsOnBaseClass()
        { 
            // Arrange
            var job = Job.FromExpression<DerivedClass>(x => x.MyMethod());
            var provider = new JobFilterAttributeFilterProvider();

            // Act
            var filters = provider.GetFilters(job);

            // Assert
            Assert.NotNull(filters.Select(f => f.Instance).Cast<MyFilterAttribute>().Single());
        }

        private class MyFilterAttribute : JobFilterAttribute
        {
        }

        [Fact]
        public void GetFilters_RetrievesNonCachedAttributesWhenConfiguredNotTo()
        {
            // Arrange
            var job = Job.FromExpression<DerivedClass>(x => x.MyMethod());
            var provider = new JobFilterAttributeFilterProvider(false);

            // Act
            var filters = provider.GetFilters(job);

            // Assert
            Assert.NotNull(filters.Select(f => f.Instance).Cast<MyFilterAttribute>().Single());
        }
    }
}
