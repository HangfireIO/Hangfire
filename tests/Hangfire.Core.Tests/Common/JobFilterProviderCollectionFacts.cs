using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobFilterProviderCollectionFacts
    {
        private readonly Job _job;

        public JobFilterProviderCollectionFacts()
        {
            _job = Job.FromExpression(() => Sample());
        }

        [Fact]
        public void GetFilters_ReturnsNull_WhenJobIsNull()
        {
            var collection = new JobFilterProviderCollection();
            var filters = collection.GetFilters(null);

            Assert.Empty(filters);
        }

        [Fact]
        public void GetFiltersUsesRegisteredProviders()
        {
            // Arrange
            var filter = new JobFilter(new Object(), JobFilterScope.Method, null);
            var provider = new Mock<IJobFilterProvider>(MockBehavior.Strict);
            var collection = new JobFilterProviderCollection(new[] { provider.Object });
            provider.Setup(p => p.GetFilters(_job)).Returns(new[] { filter });

            // Act
            IEnumerable<JobFilter> result = collection.GetFilters(_job);

            // Assert
            Assert.Same(filter, result.Single());
        }

        [Fact]
        public void GetFiltersSortsFiltersByOrderFirstThenScope()
        {
            // Arrange
            var actionFilter = new JobFilter(new Object(), JobFilterScope.Method, null);
            var controllerFilter = new JobFilter(new Object(), JobFilterScope.Type, null);
            var globalFilter = new JobFilter(new Object(), JobFilterScope.Global, null);
            var earlyActionFilter = new JobFilter(new Object(), JobFilterScope.Method, -100);
            var lateGlobalFilter = new JobFilter(new Object(), JobFilterScope.Global, 100);
            var provider = new Mock<IJobFilterProvider>(MockBehavior.Strict);
            var collection = new JobFilterProviderCollection(new[] { provider.Object });
            provider.Setup(p => p.GetFilters(_job))
                .Returns(new[] { actionFilter, controllerFilter, globalFilter, earlyActionFilter, lateGlobalFilter });

            // Act
            JobFilter[] result = collection.GetFilters(_job).ToArray();

            // Assert
            Assert.Equal(5, result.Length);
            Assert.Same(earlyActionFilter, result[0]);
            Assert.Same(globalFilter, result[1]);
            Assert.Same(controllerFilter, result[2]);
            Assert.Same(actionFilter, result[3]);
            Assert.Same(lateGlobalFilter, result[4]);
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
        private class AllowMultipleFalseAttribute : JobFilterAttribute
        {
        }

        [Fact]
        public void GetFiltersIncludesLastFilterOnlyWithAttributeUsageAllowMultipleFalse()
        {
            // Arrange
            var globalFilter = new JobFilter(new AllowMultipleFalseAttribute(), JobFilterScope.Global, null);
            var controllerFilter = new JobFilter(new AllowMultipleFalseAttribute(), JobFilterScope.Type, null);
            var actionFilter = new JobFilter(new AllowMultipleFalseAttribute(), JobFilterScope.Method, null);
            var provider = new Mock<IJobFilterProvider>(MockBehavior.Strict);
            var collection = new JobFilterProviderCollection(new[] { provider.Object });
            provider.Setup(p => p.GetFilters(_job))
                .Returns(new[] { controllerFilter, actionFilter, globalFilter });

            // Act
            IEnumerable<JobFilter> result = collection.GetFilters(_job);

            // Assert
            Assert.Same(actionFilter, result.Single());
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        private class AllowMultipleTrueAttribute : JobFilterAttribute
        {
        }

        [Fact]
        public void GetFiltersIncludesAllFiltersWithAttributeUsageAllowMultipleTrue()
        {
            // Arrange
            var globalFilter = new JobFilter(new AllowMultipleTrueAttribute(), JobFilterScope.Global, null);
            var controllerFilter = new JobFilter(new AllowMultipleTrueAttribute(), JobFilterScope.Type, null);
            var actionFilter = new JobFilter(new AllowMultipleTrueAttribute(), JobFilterScope.Method, null);
            var provider = new Mock<IJobFilterProvider>(MockBehavior.Strict);
            var collection = new JobFilterProviderCollection(new[] { provider.Object });
            provider.Setup(p => p.GetFilters(_job))
                .Returns(new[] { controllerFilter, actionFilter, globalFilter });

            // Act
            List<JobFilter> result = collection.GetFilters(_job).ToList();

            // Assert
            Assert.Same(globalFilter, result[0]);
            Assert.Same(controllerFilter, result[1]);
            Assert.Same(actionFilter, result[2]);
        }

        private class AllowMultipleCustomFilter : IJobFilter
        {
            public AllowMultipleCustomFilter(bool allowMultiple)
            {
                AllowMultiple = allowMultiple;
            }

            public bool AllowMultiple { get; private set; }
            public int Order { get { return -1; } }
        }

        [Fact]
        public void GetFiltersIncludesLastFilterOnlyWithCustomFilterAllowMultipleFalse()
        {
            // Arrange
            var globalFilter = new JobFilter(new AllowMultipleCustomFilter(false), JobFilterScope.Global, null);
            var controllerFilter = new JobFilter(new AllowMultipleCustomFilter(false), JobFilterScope.Type, null);
            var actionFilter = new JobFilter(new AllowMultipleCustomFilter(false), JobFilterScope.Method, null);
            var provider = new Mock<IJobFilterProvider>(MockBehavior.Strict);
            var collection = new JobFilterProviderCollection(new[] { provider.Object });
            provider.Setup(p => p.GetFilters(_job))
                .Returns(new[] { controllerFilter, actionFilter, globalFilter });

            // Act
            IEnumerable<JobFilter> result = collection.GetFilters(_job);

            // Assert
            Assert.Same(actionFilter, result.Single());
        }

        [Fact]
        public void GetFiltersIncludesAllFiltersWithCustomFilterAllowMultipleTrue()
        {
            // Arrange
            var globalFilter = new JobFilter(new AllowMultipleCustomFilter(true), JobFilterScope.Global, null);
            var controllerFilter = new JobFilter(new AllowMultipleCustomFilter(true), JobFilterScope.Type, null);
            var actionFilter = new JobFilter(new AllowMultipleCustomFilter(true), JobFilterScope.Method, null);
            var provider = new Mock<IJobFilterProvider>(MockBehavior.Strict);
            var collection = new JobFilterProviderCollection(new[] { provider.Object });
            provider.Setup(p => p.GetFilters(_job))
                .Returns(new[] { controllerFilter, actionFilter, globalFilter });

            // Act
            List<JobFilter> result = collection.GetFilters(_job).ToList();

            // Assert
            Assert.Same(globalFilter, result[0]);
            Assert.Same(controllerFilter, result[1]);
            Assert.Same(actionFilter, result[2]);
        }

        public static void Sample() { }
    }
}
