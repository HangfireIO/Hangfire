using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Server;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobFacts
    {
        private static readonly DateTime SomeDateTime = new DateTime(2014, 5, 30, 12, 0, 0);
        private static bool _methodInvoked;
        private static bool _disposed;

        private readonly Type _type;
        private readonly MethodInfo _method;
        private readonly string _fullPath;
        private readonly string[] _arguments;
        private readonly Mock<JobActivator> _activator;
        private readonly Mock<IJobCancellationToken> _token;
        
        public JobFacts()
        {
            _type = typeof (JobFacts);
            _method = _type.GetMethod("StaticMethod");
            _fullPath = Assembly.GetAssembly(_type).Location;
            _arguments = new string[0];

            _activator = new Mock<JobActivator> { CallBase = true };
            _token = new Mock<IJobCancellationToken>();
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
                () => new Job(_type, _method, null, null));
        }        

        [Fact]
        public void Ctor_ShouldInitializeAllProperties()
        {
            var job = new Job(_type, _method, _fullPath, _arguments);

            Assert.Same(_type, job.Type);
            Assert.Same(_method, job.Method);
            Assert.Same(_fullPath, job.Path);
            Assert.True(_arguments.SequenceEqual(job.Arguments));
        }

        [Fact]
        public void Ctor_ShouldHave_DefaultValueForArguments()
        {
            var job = new Job(_type, _method);

            Assert.Empty(job.Arguments);
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenArgumentCountIsNotEqualToParameterCount()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Job(_type, _method, new[] { "hello!" }));

            Assert.Contains("count", exception.Message);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContains_UnassignedGenericTypeParameters()
        {
            var method = _type.GetMethod("GenericMethod");

            Assert.Throws<NotSupportedException>(
                () => new Job(_type, method, new[] { "hello!" }));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodReturns_Task()
        {
            var method = _type.GetMethod("AsyncMethod");

            Assert.Throws<NotSupportedException>(
                () => new Job(_type, method, new string[0]));
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
            Assert.Throws<ArgumentException>(
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
        public void FromStaticExpression_ConvertsDateTimeRepresentation_ToIso8601Format()
        {
            var date = new DateTime(2014, 5, 30, 12, 0, 0, 777);
            var expected = date.ToString("o");

            var job = Job.FromExpression(() => MethodWithDateTimeArgument(date));

            Assert.Equal(expected, job.Arguments[0]);
        }

	    [Fact]
	    public void FromExpression_ConvertsArgumentsToJson()
	    {
		    var job = Job.FromExpression(() => MethodWithArguments("123", 1));

			Assert.Equal("\"123\"", job.Arguments[0]);
			Assert.Equal("1", job.Arguments[1]);
	    }

        [Fact]
        public void FromExpression_ReturnValueDoesNotDepend_OnCurrentCulture()
        {
            var date = DateTime.UtcNow;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var enJob = Job.FromExpression(() => MethodWithDateTimeArgument(date));

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var ruJob = Job.FromExpression(() => MethodWithDateTimeArgument(date));

            Assert.Equal(enJob.Arguments[0], ruJob.Arguments[0]);
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
            Assert.Throws<ArgumentException>(
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
        public void FromNonGenericExpression_ShouldInferType_FromAGivenObject()
        {
            IDisposable instance = new Instance();
            var job = Job.FromExpression(() => instance.Dispose());

            Assert.Equal(typeof(Instance), job.Type);
        }

        [Fact]
        public void FromNonGenericExpression_ShouldThrowAnException_IfGivenObjectIsNull()
        {
            IDisposable instance = null;

            Assert.Throws<InvalidOperationException>(
                () => Job.FromExpression(() => instance.Dispose()));
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

            var exception = Assert.Throws<ArgumentNullException>(
                () => job.Perform(null, _token.Object));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact]
        public void Perform_ThrowsAnException_WhenCancellationTokenIsNull()
        {
            var job = Job.FromExpression(() => StaticMethod());

            var exception = Assert.Throws<ArgumentNullException>(
                () => job.Perform(_activator.Object, null));

            Assert.Equal("cancellationToken", exception.ParamName);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression(() => StaticMethod());

            job.Perform(_activator.Object, _token.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform(_activator.Object, _token.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform(_activator.Object, _token.Object);

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Perform_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithArguments("hello", 5));

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTime_IfItWasSerialized_UsingTypeConverter()
        {
            // Arrange
            _methodInvoked = false;
            var typeConverter = TypeDescriptor.GetConverter(typeof (DateTime));
            var convertedDate = typeConverter.ConvertToInvariantString(SomeDateTime);

            var type = typeof (JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            var job = new Job(type, method, new[] { convertedDate });

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTime_IfItWasSerialized_UsingOldFormat()
        {
            // Arrange
            _methodInvoked = false;
            var convertedDate = SomeDateTime.ToString("MM/dd/yyyy HH:mm:ss.ffff");

            var type = typeof(JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            var job = new Job(type, method, new[] { convertedDate });

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTimeArguments()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithDateTimeArgument(SomeDateTime));

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

		[Fact, StaticLock]
	    public void Perform_WorksCorrectly_WithNullValues()
	    {
			// Arrange
			_methodInvoked = false;
			var job = Job.FromExpression(() => NullArgumentMethod(null));

			// Act
			job.Perform(_activator.Object, _token.Object);

			// Assert - see also `NullArgumentMethod` method.
			Assert.True(_methodInvoked);
	    }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorThrowsAnException()
        {
            var exception = new InvalidOperationException();
            _activator.Setup(x => x.ActivateJob(It.IsAny<Type>())).Throws(exception);

            var job = Job.FromExpression(() => InstanceMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.Same(exception, thrownException.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorReturnsNull()
        {
            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Returns(null);
            var job = Job.FromExpression(() => InstanceMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
        }

        [Fact]
        public void Ctor_ThrowsJsonReaderException_OnArgumentsDeserializationFailure()
        {
	        var type = typeof (JobFacts);
	        var method = type.GetMethod("MethodWithDateTimeArgument");

            Assert.Throws<JsonReaderException>(
                () => new Job(type, method, new []{ JobHelper.ToJson("sdfa") }));
        }

        [Fact, StaticLock]
        public void Perform_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;

            var job = Job.FromExpression<BrokenDispose>(x => x.Method());

            var exception = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.True(_methodInvoked);
            Assert.NotNull(exception.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WithUnwrappedInnerException()
        {
            var job = Job.FromExpression(() => ExceptionMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenAMethodThrowsTaskCanceledException()
        {
            var job = Job.FromExpression(() => TaskCanceledExceptionMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.IsType<TaskCanceledException>(thrownException.InnerException);
        }

        [Fact]
        public void Perform_PassesCancellationToken_IfThereIsIJobCancellationTokenParameter()
        {
            // Arrange
            var job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _token.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => job.Perform(_activator.Object, _token.Object));
        }

	    [Fact]
        public void Perform_ReturnsValue_WhenCallingFunctionReturningValue()
        {
            var job = Job.FromExpression<Instance>(x => x.FunctionReturningValue());

            var result = job.Perform(_activator.Object, _token.Object);

            Assert.Equal("Return value", result);
        }

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

        public static void StaticMethod()
        {
            _methodInvoked = true;
        }

        public void InstanceMethod()
        {
            _methodInvoked = true;
        }

        public static void CancelableJob(IJobCancellationToken token)
        {
            token.ThrowIfCancellationRequested();
        }

	    public static void NullArgumentMethod(string[] argument)
	    {
		    _methodInvoked = true;
		    Assert.Null(argument);
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

        public void MethodWithDateTimeArgument(DateTime argument)
        {
            _methodInvoked = true;

            Assert.Equal(SomeDateTime, argument);
        }

        public static void ExceptionMethod()
        {
            throw new InvalidOperationException("exception");
        }

        public static void TaskCanceledExceptionMethod()
        {
            throw new TaskCanceledException();
        }

        public void GenericMethod<T>(T arg)
        {
        }

        public Task AsyncMethod()
        {
            var source = new TaskCompletionSource<bool>();
            return source.Task;
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

            public string FunctionReturningValue()
            {
                return "Return value";
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

        public class JobClassWrapper<T> : IDisposable where T : IDisposable
        {
            public void Dispose()
            {
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
