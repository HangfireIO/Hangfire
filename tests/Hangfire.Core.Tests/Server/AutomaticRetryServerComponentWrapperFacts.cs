using System;
using System.Threading;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class AutomaticRetryServerComponentWrapperFacts
    {
        private readonly Mock<IServerComponent> _component;
        private readonly CancellationTokenSource _cts;
        private int _maxRetryAttempts;

        public AutomaticRetryServerComponentWrapperFacts()
        {
            _component = new Mock<IServerComponent>();
            _maxRetryAttempts = 3;
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerComponentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new AutomaticRetryServerComponentWrapper(null));

            Assert.Equal("innerComponent", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMaxRetryAttempts_LesserThanZero()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new AutomaticRetryServerComponentWrapper(_component.Object, -1));

            Assert.Equal("maxRetryAttempts", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenDelayCallbackIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new AutomaticRetryServerComponentWrapper(_component.Object, 1, null));

            Assert.Equal("delayCallback", exception.ParamName);
        }

        [Fact]
        public void InnerComponent_ReturnsGivenComponent()
        {
            var wrapper = CreateWrapper();

            Assert.Same(_component.Object, wrapper.InnerComponent);
        }

        [Fact]
        public void Execute_CallsComponents_ExecuteMethod()
        {
            var wrapper = CreateWrapper();

            wrapper.Execute(_cts.Token);

            _component.Verify(x => x.Execute(It.Is<CancellationToken>(y => y == _cts.Token)));
        }

        [Fact]
        public void Execute_AutomaticallyRetries_ComponentInvocation_OnException()
        {
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();
            var wrapper = CreateWrapper();

            Assert.Throws<InvalidOperationException>(() => wrapper.Execute(_cts.Token));

            _component.Verify(x => x.Execute(
                It.IsAny<CancellationToken>()),
                Times.Exactly(_maxRetryAttempts));
        }

        [Fact]
        public void Execute_ShouldNotRetry_ComponentInvocation_WhenMaxRetryAttemptsIsZero()
        {
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();
            _maxRetryAttempts = 0;
            var wrapper = CreateWrapper();
            
            Assert.Throws<InvalidOperationException>(() => wrapper.Execute(_cts.Token));

            _component.Verify(x => x.Execute(
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [PossibleHangingFact]
        public void Execute_ShouldBeInterrupted_ByCancellationToken()
        {
            var wrapper = CreateWrapper();
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();
            _cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => wrapper.Execute(_cts.Token));

            _component.Verify(x => x.Execute(It.IsAny<CancellationToken>()), Times.Once);
        }

        [PossibleHangingFact]
        public void Execute_DoesNotCauseAutomaticRetry_OnOperationCanceledException()
        {
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();
            var wrapper = CreateWrapper();

            Assert.Throws<OperationCanceledException>(() => wrapper.Execute(_cts.Token));

            _component.Verify(x => x.Execute(It.IsAny<CancellationToken>()), Times.Once);
        }

        private AutomaticRetryServerComponentWrapper CreateWrapper()
        {
            return new AutomaticRetryServerComponentWrapper(_component.Object, _maxRetryAttempts, x => TimeSpan.Zero);
        }

        private class WaitingComponent : IServerComponent
        {
            public int CalledTimes = 0;

            public void Execute(CancellationToken token)
            {
                CalledTimes++;
                token.WaitHandle.WaitOne(Timeout.Infinite);
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
