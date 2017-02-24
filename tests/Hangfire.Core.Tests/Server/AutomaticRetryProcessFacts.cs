using System;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Server;
using Moq;
using Xunit;

#pragma warning disable 618

namespace Hangfire.Core.Tests.Server
{
    public class AutomaticRetryProcessFacts
    {
        private readonly Mock<IBackgroundProcess> _process;
        private readonly BackgroundProcessContextMock _context;
        private TimeSpan _delay;
        private int _maxRetryAttempts;

        public AutomaticRetryProcessFacts()
        {
            _process = new Mock<IBackgroundProcess>();
            _delay = TimeSpan.Zero;
            _maxRetryAttempts = 3;
            _context = new BackgroundProcessContextMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerComponentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new AutomaticRetryProcess(null));

            Assert.Equal("innerProcess", exception.ParamName);
        }

        [Fact]
        public void InnerComponent_ReturnsGivenComponent()
        {
            var wrapper = CreateWrapper();

            Assert.Same(_process.Object, wrapper.InnerProcess);
        }

        [Fact]
        public void Execute_CallsTheExecuteMethod_OfAProcess()
        {
            var wrapper = CreateWrapper();

            wrapper.Execute(_context.Object);

            _process.Verify(x => x.Execute(It.IsNotNull<BackgroundProcessContext>()));
        }

        [Fact]
        public void Execute_CallsTheExecuteMethod_OfAComponent()
        {
            var component = new Mock<IServerComponent>();
            var wrapper = new AutomaticRetryProcess(component.Object);

            wrapper.Execute(_context.Object);

            component.Verify(x => x.Execute(_context.CancellationTokenSource.Token));
        }

        [Fact]
        public void Execute_AutomaticallyRetries_ComponentInvocation_OnException()
        {
            _process.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Throws<InvalidOperationException>();
            var wrapper = CreateWrapper();

            Assert.Throws<InvalidOperationException>(() => wrapper.Execute(_context.Object));

            _process.Verify(x => x.Execute(
                It.IsNotNull<BackgroundProcessContext>()),
                Times.Exactly(_maxRetryAttempts));
        }

        [Fact]
        public void Execute_ShouldNotRetry_ComponentInvocation_WhenMaxRetryAttemptsIsZero()
        {
            _process.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Throws<InvalidOperationException>();
            _maxRetryAttempts = 0;
            var wrapper = CreateWrapper();
            
            Assert.Throws<InvalidOperationException>(() => wrapper.Execute(_context.Object));

            _process.Verify(x => x.Execute(
                It.IsNotNull<BackgroundProcessContext>()),
                Times.Once);
        }

        [Fact]
        public void Execute_ShouldBeInterrupted_ByCancellationToken()
        {
            _delay = TimeSpan.FromDays(1);
            var wrapper = CreateWrapper();
            _process.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Throws<InvalidOperationException>();
            _context.CancellationTokenSource.Cancel();

            wrapper.Execute(_context.Object);

            _process.Verify(x => x.Execute(It.IsNotNull<BackgroundProcessContext>()), Times.Once);
        }

        [Fact]
        public void Execute_DoesNotCauseAutomaticRetry_WhenOperationCanceledExceptionCausedByShutdownThrown()
        {
            _context.CancellationTokenSource.Cancel();
            _process.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Throws<OperationCanceledException>();
            var wrapper = CreateWrapper();

            Assert.Throws<OperationCanceledException>(() => wrapper.Execute(_context.Object));

            _process.Verify(x => x.Execute(It.IsNotNull<BackgroundProcessContext>()), Times.Once);
        }

        [Fact]
        public void Execute_CausesAutomaticRetry_WhenOperationCanceledExceptionThrown_NotCausedByShutdown()
        {
            _process.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Throws<OperationCanceledException>();
            var wrapper = CreateWrapper();

            Assert.Throws<OperationCanceledException>(() => wrapper.Execute(_context.Object));

            _process.Verify(x => x.Execute(It.IsNotNull<BackgroundProcessContext>()), Times.Exactly(_maxRetryAttempts));
        }

        private AutomaticRetryProcess CreateWrapper()
        {
            return new AutomaticRetryProcess(_process.Object)
            {
                MaxRetryAttempts = _maxRetryAttempts,
                DelayCallback = x => _delay
            };
        }

        [UsedImplicitly]
#pragma warning disable 618
        private class WaitingComponent : IServerComponent
#pragma warning restore 618
        {
            public void Execute(CancellationToken token)
            {
                token.WaitHandle.WaitOne(Timeout.Infinite);
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
