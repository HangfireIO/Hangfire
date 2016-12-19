using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Core.Tests.Common;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class CoreBackgroundJobPerformerFacts : IDisposable
    {
        private readonly Mock<JobActivator> _activator;
        private readonly PerformContextMock _context;

        private static readonly DateTime SomeDateTime = new DateTime(2014, 5, 30, 12, 0, 0);
        private static bool _methodInvoked;
        private static bool _disposed;

        public CoreBackgroundJobPerformerFacts()
        {
            _activator = new Mock<JobActivator>() { CallBase = true };
            _context = new PerformContextMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenActivatorIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new CoreBackgroundJobPerformer(null));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => StaticMethod());
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_ActivatesJob_WithinAScope()
        {
            var performer = CreatePerformer();
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());

            performer.Perform(_context.Object);

            _activator.Verify(x => x.BeginScope(It.IsNotNull<JobActivatorContext>()), Times.Once);
        }

        [Fact, StaticLock]
        public void Perform_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            _context.BackgroundJob.Job = Job.FromExpression<Disposable>(x => x.Method());
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Perform_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => MethodWithArguments("hello", 5));
            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

#if NETFULL
        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTime_IfItWasSerialized_UsingTypeConverter()
        {
            // Arrange
            _methodInvoked = false;
            var typeConverter = TypeDescriptor.GetConverter(typeof(DateTime));
            var convertedDate = typeConverter.ConvertToInvariantString(SomeDateTime);

            var type = typeof(CoreBackgroundJobPerformerFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

#pragma warning disable CS0618 // Type or member is obsolete
            _context.BackgroundJob.Job = new Job(type, method, new [] { convertedDate });
#pragma warning restore CS0618 // Type or member is obsolete
            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }
#endif

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTime_IfItWasSerialized_UsingOldFormat()
        {
            // Arrange
            _methodInvoked = false;
            var convertedDate = SomeDateTime.ToString("MM/dd/yyyy HH:mm:ss.ffff");

            var type = typeof(CoreBackgroundJobPerformerFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

#pragma warning disable CS0618 // Type or member is obsolete
            _context.BackgroundJob.Job = new Job(type, method, new [] { convertedDate });
#pragma warning restore CS0618 // Type or member is obsolete
            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTimeArguments()
        {
            // Arrange
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => MethodWithDateTimeArgument(SomeDateTime));
            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_WorksCorrectly_WithNullValues()
        {
            // Arrange
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => NullArgumentMethod(null));

            var performer = CreatePerformer();
            // Act
            performer.Perform(_context.Object);

            // Assert - see also `NullArgumentMethod` method.
            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Perform_ThrowsException_WhenActivatorThrowsAnException()
        {
            // Arrange
            var exception = new InvalidOperationException();
            _activator.Setup(x => x.ActivateJob(It.IsAny<Type>())).Throws(exception);

            _context.BackgroundJob.Job = Job.FromExpression(() => InstanceMethod());
            var performer = CreatePerformer();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => performer.Perform(_context.Object));
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorReturnsNull()
        {
            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Returns(null);
            _context.BackgroundJob.Job = Job.FromExpression(() => InstanceMethod());
            var performer = CreatePerformer();

            Assert.Throws<InvalidOperationException>(
                () => performer.Perform(_context.Object));
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_OnArgumentsDeserializationFailure()
        {
            var type = typeof(JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");
            _context.BackgroundJob.Job = new Job(type, method, "sdfa");
            var performer = CreatePerformer();

            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.NotNull(exception.InnerException);
        }

        [Fact, StaticLock]
        public void Perform_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression<BrokenDispose>(x => x.Method());
            var performer = CreatePerformer();
            
            Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WithUnwrappedInnerException()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => ExceptionMethod());
            var performer = CreatePerformer();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }

#pragma warning disable 4014
        [Fact]
        public void Run_ThrowsPerformanceException_WithUnwrappedInnerException_ForTasks()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => TaskExceptionMethod());
            var performer = CreatePerformer();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }
#pragma warning restore 4014

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenMethodThrownTaskCanceledException()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => TaskCanceledExceptionMethod());
            var performer = CreatePerformer();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.IsType<TaskCanceledException>(thrownException.InnerException);
        }

        [Fact]
        public void Perform_RethrowsOperationCanceledException_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(new CancellationToken(true));
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_RethrowsTaskCanceledException_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(new CancellationToken(true));
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<TaskCanceledException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<TaskCanceledException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_RethrowsJobAbortedException()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<JobAbortedException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<JobAbortedException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_ThrowsJobPerformanceException_InsteadOfOperationCanceled_WhenShutdownWasNOTInitiated()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<JobPerformanceException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_PassesStandardCancellationToken_IfThereIsCancellationTokenParameter()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(default(CancellationToken)));
            var source = new CancellationTokenSource();
            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(source.Token);
            var performer = CreatePerformer();

            // Act & Assert
            source.Cancel();
            Assert.Throws<OperationCanceledException>(
                () => performer.Perform(_context.Object));
        }

        [Fact]
        public void Perform_ReturnsValue_WhenCallingFunctionReturningValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningValue());
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Equal("Return value", result);
        }

#pragma warning disable 4014
        [Fact]
        public void Run_DoesNotReturnValue_WhenCallingFunctionReturningPlainTask()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningTask());
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Equal(null, result);
        }
#pragma warning restore 4014

#pragma warning disable 4014
        [Fact]
        public void Run_ReturnsTaskResult_WhenCallingFunctionReturningGenericTask()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningTaskResultingInString());
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Equal("Return value", result);
        }
#pragma warning restore 4014

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

        public static void CancelableJob(CancellationToken token)
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

        public static async Task TaskExceptionMethod()
        {
            await Task.Yield();

            throw new InvalidOperationException("exception");
        }

        public static void TaskCanceledExceptionMethod()
        {
            throw new TaskCanceledException();
        }

        private CoreBackgroundJobPerformer CreatePerformer()
        {
            return new CoreBackgroundJobPerformer(_activator.Object);
        }
    }
}
