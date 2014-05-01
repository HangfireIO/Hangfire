using System;
using System.Linq;
using System.Reflection;
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

        private readonly Type _type;
        private readonly MethodInfo _method;
        private readonly string[] _arguments;
        private readonly Mock<JobActivator> _activator;
        
        public JobFacts()
        {
            _type = typeof (JobFacts);
            _method = _type.GetMethod("StaticMethod");
            _arguments = new string[0];

            _activator = new Mock<JobActivator> { CallBase = true };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(null, _method, _arguments));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheMethodIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(_type, null, _arguments));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeDoesNotContainTheGivenMethod()
        {
            Assert.Throws<ArgumentException>(
                () => new Job(typeof(Job), _method, _arguments));
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenArgumentsArrayIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(_type, _method, null));
        }

        [Fact]
        public void Ctor_ShouldInitializeAllProperties()
        {
            var job = new Job(_type, _method, _arguments);

            Assert.Same(_type, job.Type);
            Assert.Same(_method, job.Method);
            Assert.Same(_arguments, job.Arguments);
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenArgumentCountIsNotEqualToParameterCount()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Job(_type, _method, new[] { "hello!" }));

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
        public void FromStaticExpression_ThrowsAnException_WhenNewExpressionIsGiven()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => new JobFacts()));
        }

        [Fact]
        public void FromStaticExpression_ShouldReturnTheJob()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            Assert.Equal(typeof(Console), job.Type);
            Assert.Equal("WriteLine", job.Method.Name);
        }

        [Fact]
        public void FromInstanceExpression_ShouldThrowException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression<JobFacts>(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_ThrowsAnException_WhenNewExpressionIsGiven()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression<JobFacts>(x => new JobFacts()));
        }

        [Fact]
        public void FromInstanceExpression_ShouldReturnCorrectResult()
        {
            var job = Job.FromExpression<Instance>(x => x.Method());

            Assert.Equal(typeof(Instance), job.Type);
            Assert.Equal("Method", job.Method.Name);
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
        public void Ctor_ThrowsAnException_WhenMethodIsNotPublic()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => PrivateMethod()));
        }

        [Fact]
        public void Perform_ThrowsAnException_WhenActivatorIsNull()
        {
            var job = Job.FromExpression(() => StaticMethod());

            Assert.Throws<ArgumentNullException>(() => job.Perform(null));
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression(() => StaticMethod());

            job.Perform(_activator.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform(_activator.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform(_activator.Object);

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Perform_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithArguments("hello", 5));

            // Act
            job.Perform(_activator.Object);

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
            job.Perform(_activator.Object);

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
        public void Perform_ThrowsPerformanceException_WhenActivatorReturnsNull()
        {
            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Returns(null);
            var job = Job.FromExpression(() => StaticMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_OnArgumentsDeserializationFailure()
        {
            var job = Job.FromExpression(() => MethodWithCustomArgument(new Instance()));

            var exception = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object));

            Assert.NotNull(exception.InnerException);
        }

        [Fact, StaticLock]
        public void Perform_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;

            var job = Job.FromExpression<BrokenDispose>(x => x.Method());

            var exception = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object));

            Assert.True(_methodInvoked);
            Assert.NotNull(exception.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WithUnwrappedInnerException()
        {
            var job = Job.FromExpression(() => ExceptionMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }

        [Fact]
        public void GetTypeFilterAttributes_ReturnsCorrectAttributes()
        {
            var job = Job.FromExpression<Instance>(x => x.Method());
            var nonCachedAttributes = job.GetTypeFilterAttributes(false).ToArray();
            var cachedAttributes = job.GetTypeFilterAttributes(true).ToArray();

            Assert.Equal(1, nonCachedAttributes.Length);
            Assert.Equal(1, cachedAttributes.Length);

            Assert.True(nonCachedAttributes[0] is TestTypeAttribute);
            Assert.True(cachedAttributes[0] is TestTypeAttribute);
        }

        [Fact]
        public void GetMethodFilterAttributes_ReturnsCorrectAttributes()
        {
            var job = Job.FromExpression<Instance>(x => x.Method());
            var nonCachedAttributes = job.GetMethodFilterAttributes(false).ToArray();
            var cachedAttributes = job.GetMethodFilterAttributes(true).ToArray();

            Assert.Equal(1, nonCachedAttributes.Length);
            Assert.Equal(1, cachedAttributes.Length);

            Assert.True(nonCachedAttributes[0] is TestMethodAttribute);
            Assert.True(cachedAttributes[0] is TestMethodAttribute);
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

        public static void ExceptionMethod()
        {
            throw new InvalidOperationException("exception");
        }

        [TestType]
        public class Instance : IDisposable
        {
            [TestMethod]
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

        public class TestTypeAttribute : JobFilterAttribute
        {
        }

        public class TestMethodAttribute : JobFilterAttribute
        {
        }
    }
}
