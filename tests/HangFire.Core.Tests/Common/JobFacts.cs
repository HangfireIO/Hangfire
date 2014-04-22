using System;
using HangFire.Common;
using HangFire.Server;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Common
{
    public class JobFacts
    {
        private static bool _methodInvoked;
        private static bool _disposed;

        private readonly MethodData _methodData;
        private readonly string[] _arguments;
        private readonly Mock<JobActivator> _activator;

        public JobFacts()
        {
            _methodData = MethodData.FromExpression(() => StaticMethod());
            _arguments = new string[0];

            _activator = new Mock<JobActivator>
            {
                CallBase = false
            };
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenMethodDataIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(null, new string[0]));
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenArgumentsArrayIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(_methodData, null));
        }

        [Fact]
        public void Ctor_ShouldInitializeAllProperties()
        {
            var job = new Job(_methodData, _arguments);

            Assert.Same(_methodData, job.MethodData);
            Assert.Same(_arguments, job.Arguments);
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenArgumentCountIsNotEqualToParameterCount()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Job(_methodData, new[] { "hello!" }));

            Assert.Equal("arguments", exception.ParamName);
            Assert.Contains("count", exception.Message);
        }

        [Fact]
        public void FromStaticExpression_ShouldThrowException_WhenNullExpressionProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromStaticExpression_ShouldReturnTheJob()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            Assert.NotNull(job);
        }

        [Fact]
        public void FromInstanceExpression_ShouldThrowException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression<JobFacts>(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_ShouldReturnCorrectResult()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            var method = Job.FromExpression<Instance>(x => x.Method());

            Assert.NotNull(method);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContainsReferenceParameter()
        {
            string test = null;
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => MethodWithReferenceParameter(ref test)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContainsOutputParameter()
        {
            string test;
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => MethodWithOutputParameter(out test)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewExpressionIsGiven()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => new JobFacts()));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodIsNotPublic()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => PrivateMethod()));
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression(() => StaticMethod());

            job.Perform();

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform();

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform();

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Perform_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithArguments("hello", 5));

            // Act
            job.Perform();

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesObjectArguments_AsStrings()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithObjectArgument(5));

            // Act
            job.Perform();

            // Assert - see the `MethodWithObjectArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorThrowsAnException()
        {
            var exception = new InvalidOperationException();
            _activator.Setup(x => x.ActivateJob(It.IsAny<Type>())).Throws(exception);

            var job = Job.FromExpression(() => StaticMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object));

            Assert.Same(exception, thrownException.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_OnArgumentsDeserializationFailure()
        {
            var job = Job.FromExpression(() => MethodWithCustomArgument(new Instance()));

            var exception = Assert.Throws<JobPerformanceException>(
                () => job.Perform());

            Assert.NotNull(exception.InnerException);
        }

        [Fact, StaticLock]
        public void Perform_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;

            var job = Job.FromExpression<BrokenDispose>(x => x.Method());

            var exception = Assert.Throws<JobPerformanceException>(
                () => job.Perform());

            Assert.True(_methodInvoked);
            Assert.NotNull(exception.InnerException);
        }

        private static void PrivateMethod()
        {
        }

        public static void MethodWithReferenceParameter(ref string a)
        {
        }

        public static void MethodWithOutputParameter(out string a)
        {
            a = "hello";
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
