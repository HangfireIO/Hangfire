using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateMachineFactoryFacts
    {
        private readonly Mock<JobStorage> _storage;

        public StateMachineFactoryFacts()
        {
            _storage = new Mock<JobStorage>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StateMachineFactory(null));
        }

        [Fact]
        public void Create_ThrowsAnException_WhenConnectionIsNull()
        {
            var factory = CreateFactory();
            Assert.Throws<ArgumentNullException>(() => factory.Create(null));
        }

        [Fact]
        public void Create_ReturnsAnInstanceOfStateMachine()
        {
            var factory = CreateFactory();
            var connection = new Mock<IStorageConnection>();

            Assert.NotNull(factory.Create(connection.Object));
        }

        private StateMachineFactory CreateFactory()
        {
            return new StateMachineFactory(_storage.Object);
        }
    }
}
