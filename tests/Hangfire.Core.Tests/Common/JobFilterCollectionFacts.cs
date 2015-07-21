using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobFilterCollectionFacts
    {
        private readonly JobFilterCollection _collection = new JobFilterCollection();
        private readonly object _filterInstance = GetFilterInstance<IClientFilter>();

        public static IEnumerable<object[]> AddRejectsNonFilterInstancesData
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { "string" },
                    new object[] { 42 },
                    new object[] { new JobFilterCollectionFacts() },
                };
            }
        }

        [Fact]
        public void AddRejectsNonFilterInstances()
        {
            foreach (var instance in AddRejectsNonFilterInstancesData)
            {
                // Act + Assert
                Assert.Throws<InvalidOperationException>(() => _collection.Add(instance));
            }
        }

        [Fact]
        public void AddAcceptsFilterInstances()
        {
            // Arrange
            var filters = new object[] {
                GetFilterInstance<IClientFilter>(),
                GetFilterInstance<IServerFilter>(),
                GetFilterInstance<IClientExceptionFilter>(),
                GetFilterInstance<IServerExceptionFilter>(),
                GetFilterInstance<IApplyStateFilter>(),
                GetFilterInstance<IElectStateFilter>()
            }.ToList();

            // Act
            filters.ForEach(f => _collection.Add(f));

            // Assert
            Assert.Equal(filters, _collection.Select(i => i.Instance));
        }

        [Fact]
        public void AddPlacesFilterInGlobalScope()
        {
            // Act
            _collection.Add(_filterInstance);

            // Assert
            JobFilter filter = Assert.Single(_collection);
            Assert.Same(_filterInstance, filter.Instance);
            Assert.Equal(JobFilterScope.Global, filter.Scope);
            Assert.Equal(-1, filter.Order);
        }

        [Fact]
        public void AddWithOrderPlacesFilterInGlobalScope()
        {
            // Act
            _collection.Add(_filterInstance, 42);

            // Assert
            JobFilter filter = Assert.Single(_collection);
            Assert.Same(_filterInstance, filter.Instance);
            Assert.Equal(JobFilterScope.Global, filter.Scope);
            Assert.Equal(42, filter.Order);
        }

        [Fact]
        public void ContainsFindsFilterByInstance()
        {
            // Arrange
            _collection.Add(_filterInstance);

            // Act
            bool result = _collection.Contains(_filterInstance);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RemoveDeletesFilterByInstance()
        {
            // Arrange
            _collection.Add(_filterInstance);

            // Act
            _collection.Remove(_filterInstance);

            // Assert
            Assert.Empty(_collection);
        }

        [Fact]
        public void CollectionIsIFilterProviderWhichReturnsAllFilters()
        {
            // Arrange
            _collection.Add(_filterInstance);
            var provider = (IJobFilterProvider)_collection;

            // Act
            IEnumerable<JobFilter> result = provider.GetFilters(null);

            // Assert
            JobFilter filter = Assert.Single(result);
            Assert.Same(_filterInstance, filter.Instance);
        }

        [Fact]
        public void Count_ReturnsNumberOfElements()
        {
            _collection.Add(_filterInstance);

            Assert.Equal(1, _collection.Count);
        }

        [Fact]
        public void Clear_RemovesAllElementsFromCollection()
        {
            _collection.Add(_filterInstance);

            _collection.Clear();
            
            Assert.Equal(0, _collection.Count);
        }

        private static TFilter GetFilterInstance<TFilter>() where TFilter : class
        {
            return new Mock<TFilter>().Object;
        }
    }
}
