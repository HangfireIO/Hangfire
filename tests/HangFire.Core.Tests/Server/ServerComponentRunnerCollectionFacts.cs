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

        private ServerComponentRunnerCollection CreateCollection()
        {
            return new ServerComponentRunnerCollection(_runners);
        }
    }
}
