using System;
using System.Threading;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class InfiniteLoopComponentFacts
    {
        private readonly Mock<IServerComponent> _inner;
        private readonly CancellationTokenSource _cts;

        public InfiniteLoopComponentFacts()
        {
            _inner = new Mock<IServerComponent>();
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerComponentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new InfiniteLoopProcess(null));
        }

        [Fact]
        public void InnerComponent_ReturnsTheInnerComponent()
        {
            var component = CreateComponent();
            var result = component.InnerProcess;
            Assert.Same(_inner.Object, result);
        }

        [Fact]
        public void Execute_CallsTheExecuteMethod_UntilCancellationToken_IsCanceled()
        {
            // Arrange
            _inner.Setup(x => x.Execute(It.IsAny<CancellationToken>()))
                  .Callback(() => { Thread.Sleep(5); });

            var component = CreateComponent();
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            Assert.Throws<OperationCanceledException>(() => component.Execute(_cts.Token));

            // Assert
            _inner.Verify(x => x.Execute(_cts.Token), Times.AtLeast(5));
        }

        [Fact]
        public void Execute_DoesNotCallTheExecuteMethod_WhenCancellationToken_IsAlreadyCanceled()
        {
            // Arrange
            var component = CreateComponent();
            _cts.Cancel();

            // Act
            Assert.Throws<OperationCanceledException>(() => component.Execute(_cts.Token));

            // Assert
            _inner.Verify(x => x.Execute(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Execute_DoesNotCallExecuteMethod_WhenCancellationToken_IsCanceled()
        {
            _cts.Cancel();
        }

        [Fact]
        public void ToString_ReturnsTheName_OfInnerComponent()
        {
            var component = CreateComponent();
            var result = component.ToString();
            Assert.Equal(_inner.Object.ToString(), result);
        }

        private InfiniteLoopProcess CreateComponent()
        {
            return new InfiniteLoopProcess(_inner.Object);
        }
    }
}
