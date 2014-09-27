using Hangfire.Server;
using Moq;
using System;
using System.Threading;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerSupervisorFacts
    {
        private readonly Mock<IServerComponent> _component;
        private readonly ServerSupervisorOptions _options;

        public ServerSupervisorFacts()
        {
            _component = new Mock<IServerComponent>();
            _options = new ServerSupervisorOptions
            {
                ShutdownTimeout = Net40CompatibilityHelper.Timeout.InfiniteTimeSpan // Letting tests to timeout
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenComponentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerSupervisor(null));

            Assert.Equal("component", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerSupervisor(_component.Object, null));

            Assert.Equal("options", exception.ParamName);
        }

        [PossibleHangingFact]
        public void Ctor_UsesDefaultOptions_IfTheyWereNoProvided()
        {
            Assert.DoesNotThrow(() => new ServerSupervisor(_component.Object));
        }

        [PossibleHangingFact]
        public void Dispose_OnJustCreatedSupervisor_DoNotLeadToComponentExecution()
        {
            var supervisor = CreateSupervisor();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            supervisor.Dispose();

            _component.Verify(x => x.Execute(It.IsAny<CancellationToken>()), Times.Never);
        }

        [PossibleHangingFact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var supervisor = CreateSupervisor();
            supervisor.Dispose();

            Assert.DoesNotThrow(supervisor.Dispose);
        }

        [PossibleHangingFact]
        public void Start_LeadsToLoopedComponentExecution()
        {
            var supervisor = CreateSupervisor();

            supervisor.Start();
            Thread.Sleep(TimeSpan.FromMilliseconds(150));

            _component.Verify(
                x => x.Execute(It.IsNotNull<CancellationToken>()),
                Times.AtLeast(2));
        }

        [PossibleHangingFact]
        public void Start_OnDisposedObject_ThrowsAnException()
        {
            var supervisor = CreateSupervisor();
            supervisor.Dispose();

            Assert.Throws<ObjectDisposedException>(() => supervisor.Start());
        }

        [PossibleHangingFact]
        public void Stop_LeadsToStoppedComponentExecution()
        {
            // Arrange
            int timesExecuted = 0;

            var supervisor = CreateSupervisor();
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>()))
                .Callback(() => { timesExecuted++; Thread.Sleep(50); });

            supervisor.Start();

            // Act
            supervisor.Stop();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            timesExecuted = 0;

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.Equal(0, timesExecuted);
        }

        [PossibleHangingFact]
        public void Stop_OnDisposedObject_ThrowsAnException()
        {
            var supervisor = CreateSupervisor();
            supervisor.Dispose();

            Assert.Throws<ObjectDisposedException>(() => supervisor.Stop());
        }

        [PossibleHangingFact]
        public void Start_CanRestartStoppedComponent()
        {
            // Arrange
            int timesExecuted = 0;

            var supervisor = CreateSupervisor();
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>()))
                .Callback(() => { timesExecuted++; Thread.Sleep(50); });

            supervisor.Start();
            supervisor.Stop();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            timesExecuted = 0;

            // Act
            supervisor.Start();
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(timesExecuted > 0);
        }

        [PossibleHangingFact]
        public void Stop_CanBeCalledMultipleTimesInARow()
        {
            var supervisor = CreateSupervisor();
            supervisor.Start();
            supervisor.Stop();

            Assert.DoesNotThrow(supervisor.Stop);
        }

        [PossibleHangingFact]
        public void Dispose_StopsExecutionAutomatically()
        {
            var supervisor = CreateSupervisor();
            supervisor.Start();

            Assert.DoesNotThrow(supervisor.Dispose);
        }

        [PossibleHangingFact]
        public void Dispose_CanBeCalled_AfterStop()
        {
            var supervisor = CreateSupervisor();
            supervisor.Start();
            supervisor.Stop();

            Assert.DoesNotThrow(supervisor.Dispose);
        }

        [PossibleHangingFact]
        public void Dispose_ShouldDisposeDisposableComponent()
        {
            // Arrange
            var component = new DisposableComponent();
            var supervisor = new ServerSupervisor(component, _options);

            supervisor.Start();
            Thread.Sleep(100);

            // Act
            supervisor.Dispose();
            Thread.Sleep(100);

            // Assert
            Assert.True(component.Disposed);
        }

        [Fact]
        public void Component_ReturnsUnderlyingComponent()
        {
            var supervisor = CreateSupervisor();

            Assert.Same(_component.Object, supervisor.Component);
        }

        private ServerSupervisor CreateSupervisor()
        {
            _component.Setup(x => x.Execute(It.IsAny<CancellationToken>()))
                .Callback(() => Thread.Sleep(50));
            return new ServerSupervisor(_component.Object, _options);
        }

        private class DisposableComponent : IServerComponent, IDisposable
        {
            public bool Disposed { get; set; }

            public void Execute(CancellationToken cancellationToken)
            {
                Thread.Yield();
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
