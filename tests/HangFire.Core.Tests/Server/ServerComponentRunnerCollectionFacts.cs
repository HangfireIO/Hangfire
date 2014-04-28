using System;
using System.Collections.Generic;
using HangFire.Server;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerComponentRunnerCollectionFacts
    {
        private readonly Mock<IServerComponentRunner> _runner1;
        private readonly Mock<IServerComponentRunner> _runner2;
        private readonly List<IServerComponentRunner> _runners;

        public ServerComponentRunnerCollectionFacts()
        {
            _runner1 = new Mock<IServerComponentRunner>();
            _runner2 = new Mock<IServerComponentRunner>();

            _runners = new List<IServerComponentRunner>
            {
                _runner1.Object,
                _runner2.Object
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenRunnersValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerComponentRunnerCollection(null));
        }

        [Fact]
        public void Start_ExecutesStartMethod_OnAllRegisteredComponents()
        {
            var collection = CreateCollection();
            
            collection.Start();

            _runner1.Verify(x => x.Start());
            _runner2.Verify(x => x.Start());
        }

        [Fact]
        public void Stop_ExecutesStopMethod_OnAllRegisteredComponents()
        {
            var collection = CreateCollection();

            collection.Stop();

            _runner1.Verify(x => x.Stop());
            _runner2.Verify(x => x.Stop());
        }

        [Fact]
        public void Dispose_InvokesDisposeMethod_OnAllRegisteredComponents()
        {
            var collection = CreateCollection();

            collection.Dispose();

            _runner1.Verify(x => x.Dispose());
            _runner2.Verify(x => x.Dispose());
        }

        [Fact]
        public void Dispose_AlsoInvokesStopMethod_OnAllRegisteredComponents()
        {
            var collection = CreateCollection();

            collection.Dispose();

            _runner1.Verify(x => x.Stop());
            _runner2.Verify(x => x.Stop());
        }

        [Fact]
        public void Count_ReturnsTheNumberOfElements()
        {
            var collection = CreateCollection();

            Assert.Equal(2, collection.Count);
        }

        [Fact]
        public void Add_AddsNewElement()
        {
            var collection = CreateCollection();
            
            collection.Add(new Mock<IServerComponentRunner>().Object);

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public void Clear_RemovesAllElements_FromCollection()
        {
            var collection = CreateCollection();
            
            collection.Clear();
            
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void Contains_ReturnsWhetherElementIsInCollection()
        {
            var element = new Mock<IServerComponentRunner>();
            var collection = CreateCollection();

            Assert.False(collection.Contains(element.Object));

            collection.Add(element.Object);

            Assert.True(collection.Contains(element.Object));
        }

        [Fact]
        public void Remove_RemovesGivenElementFromCollection()
        {
            var runner = new Mock<IServerComponentRunner>();
            var collection = CreateCollection();
            collection.Add(runner.Object);

            collection.Remove(runner.Object);

            Assert.False(collection.Contains(runner.Object));
        }

        private ServerComponentRunnerCollection CreateCollection()
        {
            return new ServerComponentRunnerCollection(_runners);
        }
    }
}
