using System;
using Hangfire.Common;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobFilterFacts
    {
        [Fact]
        public void GuardClause()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobFilter(null, JobFilterScope.Method, null));

            Assert.Equal("instance", exception.ParamName);
        }

        [Fact]
        public void FilterDoesNotImplementIJobFilter()
        {
            // Arrange
            var filterInstance = new object();

            // Act
            var filter = new JobFilter(filterInstance, JobFilterScope.Method, null);

            // Assert
            Assert.Same(filterInstance, filter.Instance);
            Assert.Equal(JobFilterScope.Method, filter.Scope);
            Assert.Equal(JobFilter.DefaultOrder, filter.Order);
        }

        [Fact]
        public void FilterImplementsIJobFilter()
        {
            // Arrange
            var filterInstance = new Mock<IJobFilter>();
            filterInstance.SetupGet(f => f.Order).Returns(42);

            // Act
            var filter = new JobFilter(filterInstance.Object, JobFilterScope.Type, null);

            // Assert
            Assert.Same(filterInstance.Object, filter.Instance);
            Assert.Equal(JobFilterScope.Type, filter.Scope);
            Assert.Equal(42, filter.Order);
        }

        [Fact]
        public void ExplicitOrderOverridesIJobFilter()
        {
            // Arrange
            var filterInstance = new Mock<IJobFilter>();
            filterInstance.SetupGet(f => f.Order).Returns(42);

            // Act
            var filter = new JobFilter(filterInstance.Object, JobFilterScope.Type, 2112);

            // Assert
            Assert.Same(filterInstance.Object, filter.Instance);
            Assert.Equal(JobFilterScope.Type, filter.Scope);
            Assert.Equal(2112, filter.Order);
        }
    }
}
