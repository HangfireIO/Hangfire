using System;
using System.ComponentModel;
using Hangfire.Common;
using Hangfire.Core.Tests.Common;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class MethodInvokePerformanceProcessFacts : IDisposable
    {
        private readonly Mock<JobActivator> _activator;
        private readonly PerformContextMock _context;

        private static readonly DateTime SomeDateTime = new DateTime(2014, 5, 30, 12, 0, 0);
        private static bool _methodInvoked;
        private static bool _disposed;

        public MethodInvokePerformanceProcessFacts()
        {
            _activator = new Mock<JobActivator>() { CallBase = true };
            _context = new PerformContextMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenActivatorIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new MethodInvokePerformanceProcess(null));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact, StaticLock]
        public void Run_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => StaticMethod());
            var process = CreateProcess();

            process.Run(_context.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression<MethodInvokePerformanceProcessFacts>(x => x.InstanceMethod());
            var process = CreateProcess();

            process.Run(_context.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            _context.BackgroundJob.Job = Job.FromExpression<Disposable>(x => x.Method());
            var process = CreateProcess();

            process.Run(_context.Object);

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Run_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => MethodWithArguments("hello", 5));
            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_PassesCorrectDateTime_IfItWasSerialized_UsingTypeConverter()
        {
            // Arrange
            _methodInvoked = false;
            var typeConverter = TypeDescriptor.GetConverter(typeof(DateTime));
            var convertedDate = typeConverter.ConvertToInvariantString(SomeDateTime);

            var type = typeof(MethodInvokePerformanceProcessFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            _context.BackgroundJob.Job = new Job(type, method, new[] { convertedDate });
            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_PassesCorrectDateTime_IfItWasSerialized_UsingOldFormat()
        {
            // Arrange
            _methodInvoked = false;
            var convertedDate = SomeDateTime.ToString("MM/dd/yyyy HH:mm:ss.ffff");

            var type = typeof(MethodInvokePerformanceProcessFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            _context.BackgroundJob.Job = new Job(type, method, new[] { convertedDate });
            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_PassesCorrectDateTimeArguments()
        {
            // Arrange
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => MethodWithDateTimeArgument(SomeDateTime));
            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_WorksCorrectly_WithNullValues()
        {
            // Arrange
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => NullArgumentMethod(null));

            var process = CreateProcess();
            // Act
            process.Run(_context.Object);

            // Assert - see also `NullArgumentMethod` method.
            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Run_ThrowsException_WhenActivatorThrowsAnException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _activator.Setup(x => x.ActivateJob(It.IsAny<Type>())).Throws(exception);

            _context.BackgroundJob.Job = Job.FromExpression(() => InstanceMethod());
            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));
        }

        [Fact]
        public void Run_ThrowsPerformanceException_WhenActivatorReturnsNull()
        {
            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Returns(null);
            _context.BackgroundJob.Job = Job.FromExpression(() => InstanceMethod());
            var process = CreateProcess();

            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));
        }

        [Fact]
        public void Run_ThrowsPerformanceException_OnArgumentsDeserializationFailure()
        {
            var type = typeof(JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");
            _context.BackgroundJob.Job = new Job(type, method, new[] { "sdfa" });
            var process = CreateProcess();

            var exception = Assert.Throws<JobPerformanceException>(
                () => process.Run(_context.Object));

            Assert.NotNull(exception.InnerException);
        }

        [Fact, StaticLock]
        public void Run_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression<BrokenDispose>(x => x.Method());
            var process = CreateProcess();
            
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));

            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Run_ThrowsPerformanceException_WithUnwrappedInnerException()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => ExceptionMethod());
            var process = CreateProcess();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => process.Run(_context.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }

        [Fact]
        public void Run_PassesCancellationToken_IfThereIsIJobCancellationTokenParameter()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();
            var process = CreateProcess();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => process.Run(_context.Object));
        }

        [Fact]
        public void Run_ReturnsValue_WhenCallingFunctionReturningValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningValue());
            var process = CreateProcess();

            var result = process.Run(_context.Object);

            Assert.Equal("Return value", result);
        }

        public void InstanceMethod()
        {
            _methodInvoked = true;
        }

        public class Disposable : IDisposable
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

        public void Dispose()
        {
            _disposed = true;
        }

        public static void NullArgumentMethod(string[] argument)
        {
            _methodInvoked = true;
            Assert.Null(argument);
        }

        public static void CancelableJob(IJobCancellationToken token)
        {
            token.ThrowIfCancellationRequested();
        }

        public void MethodWithDateTimeArgument(DateTime argument)
        {
            _methodInvoked = true;

            Assert.Equal(SomeDateTime, argument);
        }

        public static void StaticMethod()
        {
            _methodInvoked = true;
        }

        public void MethodWithArguments(string stringArg, int intArg)
        {
            _methodInvoked = true;

            Assert.Equal("hello", stringArg);
            Assert.Equal(5, intArg);
        }

        public static void ExceptionMethod()
        {
            throw new InvalidOperationException("exception");
        }

        private MethodInvokePerformanceProcess CreateProcess()
        {
            return new MethodInvokePerformanceProcess(_activator.Object);
        }
    }
}
