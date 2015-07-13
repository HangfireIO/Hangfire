using System;
using System.Threading;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class InfiniteLoopComponentFacts
    {
        private readonly Mock<IServerComponent> _innerComponent;
        private readonly Mock<IBackgroundProcess> _innerProcess;
        private readonly BackgroundProcessContextMock _context;

        public InfiniteLoopComponentFacts()
        {
            _innerComponent = new Mock<IServerComponent>();
            _innerProcess = new Mock<IBackgroundProcess>();
            _context = new BackgroundProcessContextMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerComponentIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new InfiniteLoopProcess(null));
        }

        [Fact]
        public void InnerComponent_ReturnsTheInnerProcess()
        {
            var process = CreateProcess(_innerComponent.Object);
            var result = process.InnerProcess;
            Assert.Same(_innerComponent.Object, result);
        }

        [Fact]
        public void Execute_CallsTheExecuteMethod_OfAComponent_UntilCancellationToken_IsCanceled()
        {
            // Arrange
            _innerComponent.Setup(x => x.Execute(It.IsAny<CancellationToken>()))
                  .Callback(() => { Thread.Sleep(5); });

            var process = CreateProcess(_innerComponent.Object);
            _context.CancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            Assert.Throws<OperationCanceledException>(() => process.Execute(_context.Object));

            // Assert
            _innerComponent.Verify(x => x.Execute(_context.CancellationTokenSource.Token), Times.AtLeast(5));
        }

        [Fact]
        public void Execute_CallsTheExecuteMethod_OfAProcess_UntilCancellationToken_IsCanceled()
        {
            // Arrange
            _innerProcess.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>()))
                  .Callback(() => { Thread.Sleep(5); });

            var process = CreateProcess(_innerProcess.Object);
            _context.CancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            Assert.Throws<OperationCanceledException>(() => process.Execute(_context.Object));

            // Assert
            _innerProcess.Verify(x => x.Execute(_context.Object), Times.AtLeast(5));
        }

        [Fact]
        public void Execute_DoesNotCallTheExecuteMethod_WhenCancellationToken_IsAlreadyCanceled()
        {
            // Arrange
            var process = CreateProcess(_innerComponent.Object);
            _context.CancellationTokenSource.Cancel();

            // Act
            Assert.Throws<OperationCanceledException>(() => process.Execute(_context.Object));

            // Assert
            _innerComponent.Verify(x => x.Execute(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void ToString_ReturnsTheName_OfInnerComponent()
        {
            var process = CreateProcess(_innerComponent.Object);
            var result = process.ToString();
            Assert.Equal(_innerComponent.Object.ToString(), result);
        }

        private InfiniteLoopProcess CreateProcess(ILongRunningProcess process)
        {
            return new InfiniteLoopProcess(process);
        }
    }
}
