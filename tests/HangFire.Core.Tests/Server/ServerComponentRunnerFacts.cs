using System;
using System.Threading;
using HangFire.Server;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerComponentRunnerFacts
    {
        private readonly Mock<IServerComponent> _component;
        private readonly ServerComponentRunnerOptions _options;

        public ServerComponentRunnerFacts()
        {
            _component = new Mock<IServerComponent>();
            _options = new ServerComponentRunnerOptions
            {
                ShutdownTimeout = TimeSpan.Zero // Letting tests to timeout
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenComponentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerComponentRunner(null));

            Assert.Equal("component", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerComponentRunner(_component.Object, null));

            Assert.Equal("options", exception.ParamName);
        }

        [PossibleHangingFact]
        public void Ctor_UsesDefaultOptions_IfTheyWereNoProvided()
        {
            Assert.DoesNotThrow(() => new ServerComponentRunner(_component.Object));
        }

        [PossibleHangingFact]
        public void Dispose_OnJustCreatedRunner_DoNotLeadToComponentExecution()
        {
            var runner = CreateRunner();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            runner.Dispose();

            _component.Verify(x => x.Execute(It.IsAny<CancellationToken>()), Times.Never);
        }

        [PossibleHangingFact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var runner = CreateRunner();
            runner.Dispose();

            Assert.DoesNotThrow(runner.Dispose);
        }

        [PossibleHangingFact]
        public void Start_LeadsToLoopedComponentExecution()
        {
            var runner = CreateRunner();

            runner.Start();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            _component.Verify(
                x => x.Execute(It.IsNotNull<CancellationToken>()),
                Times.AtLeast(2));
        }

        [PossibleHangingFact]
        public void Start_OnDisposedObject_ThrowsAnException()
        {
            var runner = CreateRunner();
            runner.Dispose();

            Assert.Throws<ObjectDisposedException>(() => runner.Start());
        }

        [PossibleHangingFact]
        public void Stop_LeadsToStoppedComponentExecution()
        {
            // Arrange
            int timesExecuted = 0;
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Callback(() => timesExecuted++);

            var runner = CreateRunner();
            runner.Start();

            // Act
            runner.Stop();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            timesExecuted = 0;

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.Equal(0, timesExecuted);
        }

        [PossibleHangingFact]
        public void Stop_OnDisposedObject_ThrowsAnException()
        {
            var runner = CreateRunner();
            runner.Dispose();

            Assert.Throws<ObjectDisposedException>(() => runner.Stop());
        }

        [PossibleHangingFact]
        public void Start_CanRestartStoppedComponent()
        {
            // Arrange
            int timesExecuted = 0;
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>()))
                .Callback(() => timesExecuted++);

            var runner = CreateRunner();
            runner.Start();
            runner.Stop();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            timesExecuted = 0;

            // Act
            runner.Start();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(timesExecuted > 0);
        }

        [PossibleHangingFact]
        public void Stop_CanBeCalledMultipleTimesInARow()
        {
            var runner = CreateRunner();
            runner.Start();
            runner.Stop();

            Assert.DoesNotThrow(runner.Stop);
        }

        [PossibleHangingFact]
        public void Dispose_StopsExecutionAutomatically()
        {
            var runner = CreateRunner();
            runner.Start();

            Assert.DoesNotThrow(runner.Dispose);
        }

        [PossibleHangingFact]
        public void Dispose_CanBeCalled_AfterStop()
        {
            var runner = CreateRunner();
            runner.Start();
            runner.Stop();

            Assert.DoesNotThrow(runner.Dispose);
        }

        [PossibleHangingFact]
        public void Dispose_ShouldDisposeDisposableComponent()
        {
            // Arrange
            _options.MinimumLogVerbosity = true;
            _options.MaxRetryAttempts = 0;

            var component = new DisposableComponent();
            var runner = new ServerComponentRunner(component, _options);

            runner.Start();
            Thread.Sleep(100);

            // Act
            runner.Dispose();
            Thread.Sleep(500);

            // Assert
            Assert.True(component.Disposed);
        }

        [PossibleHangingFact]
        public void FailingComponent_ShouldNotBeRetried_IfMaxRetryAttemptsIsZero()
        {
            // Arrange
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();
            _options.MaxRetryAttempts = 0;

            var runner = CreateRunner();
            runner.Start();
            Thread.Sleep(500);

            // Act
            runner.Dispose();

            _component.Verify(
                x => x.Execute(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [PossibleHangingFact]
        public void FailingComponent_ShouldBeExecutedSeveralTimes_Automatically()
        {
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();
            var runner = CreateRunner();
            runner.Start();

            Thread.Sleep(500);
            runner.Dispose();

            _component.Verify(x => x.Execute(
                It.IsAny<CancellationToken>()),
                Times.AtLeast(2));
        }

        private ServerComponentRunner CreateRunner()
        {
            return new ServerComponentRunner(_component.Object, _options);
        }

        private class DisposableComponent : IServerComponent, IDisposable
        {
            public bool Disposed { get; set; }

            public void Execute(CancellationToken cancellationToken)
            {
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
