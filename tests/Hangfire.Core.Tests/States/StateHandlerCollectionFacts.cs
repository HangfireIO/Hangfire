using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateHandlerCollectionFacts
    {
        private readonly StateHandlerCollection _collection;

        public StateHandlerCollectionFacts()
        {
            _collection = new StateHandlerCollection();
        }

        [Fact]
        public void AddHandler_ThrowsAnException_WhenHandlerIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _collection.AddHandler(null));
        }

        [Fact]
        public void AddHandler_ThrowsAnException_WhenStateNameOfTheGivenHandlerIsNull()
        {
            var handler = new Mock<IStateHandler>();
            handler.Setup(x => x.StateName).Returns((string)null);

            var exception = Assert.Throws<ArgumentException>(
                () => _collection.AddHandler(handler.Object));

            Assert.Contains("StateName", exception.Message);
        }

        [Fact]
        public void GetHandlers_ReturnsEmptyCollection_WhenHandlersWereNotAddedForTheState()
        {
            var handlers = _collection.GetHandlers("State");
            Assert.Empty(handlers);
        }

        [Fact]
        public void GetHandlers_ReturnsEmptyCollection_WhenStateNameIsNull()
        {
            var handlers = _collection.GetHandlers(null);
            Assert.Empty(handlers);
        }

        [Fact]
        public void GetHandlers_ReturnsAllRegisteredHandlersForTheState()
        {
            var handler1Mock = new Mock<IStateHandler>();
            handler1Mock.Setup(x => x.StateName).Returns("State");

            var handler2Mock = new Mock<IStateHandler>();
            handler2Mock.Setup(x => x.StateName).Returns("State");

            _collection.AddHandler(handler1Mock.Object);
            _collection.AddHandler(handler2Mock.Object);

            var handlers = _collection.GetHandlers("State").ToArray();

            Assert.Contains(handler1Mock.Object, handlers);
            Assert.Contains(handler2Mock.Object, handlers);
        }

        [Fact]
        public void GetHandlers_ReturnsOnlyHandlersOfASpecifiedState()
        {
            var anotherStateHandlerMock = new Mock<IStateHandler>();
            anotherStateHandlerMock.Setup(x => x.StateName).Returns("AnotherState");

            _collection.AddHandler(anotherStateHandlerMock.Object);
            var handlers = _collection.GetHandlers("State");

            Assert.Empty(handlers);
        }

        [Fact]
        public void AddRange_ThrowsAnException_WhenEnumerationIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _collection.AddRange(null));
        }

        [Fact]
        public void AddRange_AddsHandlers_FromEnumeration()
        {
            // Arrange
            var handler1 = new Mock<IStateHandler>();
            handler1.Setup(x => x.StateName).Returns("State1");

            var handler2 = new Mock<IStateHandler>();
            handler2.Setup(x => x.StateName).Returns("State2");

            var handlers = new List<IStateHandler> { handler1.Object, handler2.Object };

            // Act
            _collection.AddRange(handlers);

            // Assert
            Assert.Same(handler1.Object, _collection.GetHandlers("State1").Single());
            Assert.Same(handler2.Object, _collection.GetHandlers("State2").Single());
        }
    }
}
