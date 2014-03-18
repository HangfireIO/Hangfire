using System;
using System.Linq;
using HangFire.Common.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
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
            var handler = new Mock<StateHandler>();
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
            var handler1Mock = new Mock<StateHandler>();
            handler1Mock.Setup(x => x.StateName).Returns("State");

            var handler2Mock = new Mock<StateHandler>();
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
            var anotherStateHandlerMock = new Mock<StateHandler>();
            anotherStateHandlerMock.Setup(x => x.StateName).Returns("AnotherState");

            _collection.AddHandler(anotherStateHandlerMock.Object);
            var handlers = _collection.GetHandlers("State");

            Assert.Empty(handlers);
        }
    }
}
