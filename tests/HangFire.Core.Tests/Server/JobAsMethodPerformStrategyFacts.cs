using System;
using HangFire.Common;
using HangFire.Server;
using HangFire.Server.Performing;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class JobAsMethodPerformStrategyFacts
    {
        private static bool _methodInvoked;
        private static bool _disposed;
        private Mock<JobActivator> _activator;

        public JobAsMethodPerformStrategyFacts()
        {
            _activator = new Mock<JobActivator>
            {
                CallBase = false
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobAsMethodPerformStrategy(null));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenActivatorIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobAsMethodPerformStrategy(
                    Job.FromExpression(() => StaticMethod()),
                    null));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeStaticMethods()
        {
            _methodInvoked = false;

            var job = Job.FromExpression(() => StaticMethod());
            var performer = new JobAsMethodPerformStrategy(job);

            performer.Perform();

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;

            var job = Job.FromExpression<Instance>(x => x.Method());
            var performer = new JobAsMethodPerformStrategy(job);

            performer.Perform();

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;

            var job = Job.FromExpression<Instance>(x => x.Method());
            var performer = new JobAsMethodPerformStrategy(job);

            performer.Perform();

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Perform_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;

            var job = Job.FromExpression(() => MethodWithArguments("hello", 5));
            var performer = new JobAsMethodPerformStrategy(job);

            // Act
            performer.Perform();

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesObjectArguments_AsStrings()
        {
            // Arrange
            _methodInvoked = false;

            var job = Job.FromExpression(() => MethodWithObjectArgument(5));
            var performer = new JobAsMethodPerformStrategy(job);

            // Act
            performer.Perform();

            // Assert - see the `MethodWithObjectArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorThrowsAnException()
        {
            var exception = new InvalidOperationException();
            _activator.Setup(x => x.ActivateJob(It.IsAny<Type>())).Throws(exception);

            var performer = CreatePerformer();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => performer.Perform());

            Assert.Same(exception, thrownException.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_OnArgumentsDeserializationFailure()
        {
            var job = Job.FromExpression(() => MethodWithCustomArgument(new Instance()));
            var performer = new JobAsMethodPerformStrategy(job);

            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform());

            Assert.NotNull(exception.InnerException);
        }

        [Fact, StaticLock]
        public void Perform_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;

            var job = Job.FromExpression<BrokenDispose>(x => x.Method());
            var performer = new JobAsMethodPerformStrategy(job);

            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform());

            Assert.True(_methodInvoked);
            Assert.NotNull(exception.InnerException);
        }

        private JobAsMethodPerformStrategy CreatePerformer()
        {
            return new JobAsMethodPerformStrategy(
                Job.FromExpression(() => StaticMethod()),
                _activator.Object);
        }

        public void StaticMethod()
        {
            _methodInvoked = true;
        }

        public void MethodWithArguments(string stringArg, int intArg)
        {
            _methodInvoked = true;

            Assert.Equal("hello", stringArg);
            Assert.Equal(5, intArg);
        }

        public void MethodWithObjectArgument(object argument)
        {
            _methodInvoked = true;

            Assert.Equal("5", argument);
        }

        public void MethodWithCustomArgument(Instance argument)
        {
        }

        public class Instance : IDisposable
        {
            public void Method()
            {
                _methodInvoked = true;
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }

        public class BrokenDispose : IDisposable
        {
            public void Method()
            {
                _methodInvoked = true;
            }

            public void Dispose()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
