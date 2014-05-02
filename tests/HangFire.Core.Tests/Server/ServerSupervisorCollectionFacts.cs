using System;
using System.Collections.Generic;
using HangFire.Server;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerSupervisorCollectionFacts
    {
        private readonly Mock<IServerSupervisor> _supervisor1;
        private readonly Mock<IServerSupervisor> _supervisor2;
        private readonly List<IServerSupervisor> _supervisors;

        public ServerSupervisorCollectionFacts()
        {
            _supervisor1 = new Mock<IServerSupervisor>();
            _supervisor2 = new Mock<IServerSupervisor>();

            _supervisors = new List<IServerSupervisor>
            {
                _supervisor1.Object,
                _supervisor2.Object
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenSupervisorsValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerSupervisorCollection(null));
        }

        [Fact]
        public void Start_ExecutesStartMethod_OnAllRegisteredSupervisors()
        {
            var collection = CreateCollection();
            
            collection.Start();

            _supervisor1.Verify(x => x.Start());
            _supervisor2.Verify(x => x.Start());
        }

        [Fact]
        public void Stop_ExecutesStopMethod_OnAllRegisteredSupervisors()
        {
            var collection = CreateCollection();

            collection.Stop();

            _supervisor1.Verify(x => x.Stop());
            _supervisor2.Verify(x => x.Stop());
        }

        [Fact]
        public void Dispose_InvokesDisposeMethod_OnAllRegisteredComponents()
        {
            var collection = CreateCollection();

            collection.Dispose();

            _supervisor1.Verify(x => x.Dispose());
            _supervisor2.Verify(x => x.Dispose());
        }

        [Fact]
        public void Dispose_AlsoInvokesStopMethod_OnAllRegisteredSupervisors()
        {
            var collection = CreateCollection();

            collection.Dispose();

            _supervisor1.Verify(x => x.Stop());
            _supervisor2.Verify(x => x.Stop());
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
            
            collection.Add(new Mock<IServerSupervisor>().Object);

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
            var element = new Mock<IServerSupervisor>();
            var collection = CreateCollection();

            Assert.False(collection.Contains(element.Object));

            collection.Add(element.Object);

            Assert.True(collection.Contains(element.Object));
        }

        [Fact]
        public void Remove_RemovesGivenElementFromCollection()
        {
            var supervisor = new Mock<IServerSupervisor>();
            var collection = CreateCollection();
            collection.Add(supervisor.Object);

            collection.Remove(supervisor.Object);

            Assert.False(collection.Contains(supervisor.Object));
        }

        [Fact]
        public void IsReadOnly_ReturnsFalse()
        {
            var collection = CreateCollection();

            Assert.False(collection.IsReadOnly);
        }

        [Fact]
        public void CopyTo_WorksAsExpected()
        {
            var collection = CreateCollection();
            var array = new IServerSupervisor[3];

            collection.CopyTo(array, 1);

            Assert.Same(_supervisor1.Object, array[1]);
            Assert.Same(_supervisor2.Object, array[2]);
        }

        private ServerSupervisorCollection CreateCollection()
        {
            return new ServerSupervisorCollection(_supervisors);
        }
    }
}
