using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerComponentExtensionsFacts
    {
        private readonly Mock<IServerComponent> _component;
        private readonly CancellationTokenSource _cts;

        public ServerComponentExtensionsFacts()
        {
            _component = new Mock<IServerComponent>();
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public void CreateTask_ThrowsAnException_WhenComponentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => ServerComponentExtensions.CreateTask(null, _cts.Token));

            Assert.Equal("component", exception.ParamName);
        }

        [Fact]
        public void CreateTask_ReturnsALongRunningTask()
        {
            var task = _component.Object.CreateTask(_cts.Token);

            Assert.True(task.CreationOptions.HasFlag(TaskCreationOptions.LongRunning));
        }

        [Fact]
        public void CreateTask_ReturnsATask_ThatCallsTheExecuteMethod_OfAGivenComponent()
        {
            var task = _component.Object.CreateTask(_cts.Token);

            task.Wait();
            
            _component.Verify(x => x.Execute(_cts.Token), Times.Once);
        }

        [Fact]
        public void CreateTask_ReturnsATask_ThatDoesNotThrowAnyException()
        {
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();
            var task = _component.Object.CreateTask(_cts.Token);

            Assert.DoesNotThrow(() => task.Wait());
        }
    }
}
