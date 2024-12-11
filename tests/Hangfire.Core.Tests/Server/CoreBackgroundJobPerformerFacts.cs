using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Core.Tests.Common;
using Hangfire.Server;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class CoreBackgroundJobPerformerFacts : IDisposable
    {
        private readonly Mock<JobActivator> _activator = new Mock<JobActivator>() { CallBase = true };
        private readonly PerformContextMock _context = new PerformContextMock();

        private static readonly DateTime SomeDateTime = new DateTime(2014, 5, 30, 12, 0, 0);
        private static bool _methodInvoked;
        private static bool _disposed;

        [Fact]
        public void Ctor_ThrowsAnException_WhenActivatorIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new CoreBackgroundJobPerformer(null, null));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenTaskSchedulerIsNull()
        {
            var performer = new CoreBackgroundJobPerformer(_activator.Object, null);
            Assert.NotNull(performer);
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

#if !NETCOREAPP1_0
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
            
            Assert.Throws<InvalidOperationException>(
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
            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(new CancellationToken(true));
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<JobAbortedException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<JobAbortedException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void ThrowsJobPerformanceException_DoesInclude_JobId()
        {
	        // Arrange
	        _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
	        _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);
	        _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();

	        var performer = CreatePerformer();

	        // Act & Assert
	        var exception = Assert.Throws<JobPerformanceException>(() => performer.Perform(_context.Object));
	        Assert.Equal(_context.BackgroundJob.Id, exception.JobId);
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

        [Fact]
        public void Run_DoesNotReturnValue_WhenCallingFunctionReturningPlainTask()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningTask());
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Null(result);
        }

        [Fact]
        public void Run_DoesNotReturnValue_WhenCallingFunctionReturningValueTask()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningValueTask());
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Run_ReturnsTaskResult_WhenCallingFunctionReturningGenericTask(bool continueOnCapturedContext)
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningTaskResultingInString(continueOnCapturedContext));
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Equal("Return value", result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Run_ReturnsTaskResult_WhenCallingFunctionReturningValueTask(bool continueOnCapturedContext)
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningValueTaskResultingInString(continueOnCapturedContext));
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Equal("Return value", result);
        }

        [Fact]
        public void Perform_ExecutesAsyncMethod_AlwaysWithinTheSameThread()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            _context.BackgroundJob.Job = Job.FromExpression(() => AsyncMethod(Thread.CurrentThread.ManagedThreadId));
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.True((bool)result);
        }

        [Fact]
        public void Perform_ExecutesAsyncMethod_OnCustomScheduler_WhenItIsSet()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            var scheduler = new MyCustomTaskScheduler();

            _context.BackgroundJob.Job = Job.FromExpression(() => TaskExceptionMethod());
            var performer = CreatePerformer(scheduler);

            Assert.Throws<JobPerformanceException>(() => performer.Perform(_context.Object));
            
            Assert.True(scheduler.TasksPerformed > 1);
        }

#if !NET452
        private static readonly AsyncLocal<string> AsyncLocal = new AsyncLocal<string>();
        
        [Fact]
        public void Perform_FlowsExistingAsyncLocal_WhenInvokingSynchronousMethod()
        {
            InvokeJobMethodWithFlowOfAsyncLocal(Job.FromExpression(() => CheckSynchronousAsyncLocal()));
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void CheckSynchronousAsyncLocal()
        {
            Assert.Equal("world", AsyncLocal.Value);
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_FlowsExistingAsyncLocal_WhenInvokingSimpleAsyncMethod(TaskScheduler scheduler)
        {
            InvokeJobMethodWithFlowOfAsyncLocal(Job.FromExpression(() => CheckSimpleAsyncLocal()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static Task CheckSimpleAsyncLocal()
        {
            Assert.Equal("world", AsyncLocal.Value);
            return Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_FlowsExistingAsyncLocal_WhenInvokingAsyncMethodWithAwait(TaskScheduler scheduler)
        {
            InvokeJobMethodWithFlowOfAsyncLocal(Job.FromExpression(() => CheckAsyncAwaitAsyncLocal()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task CheckAsyncAwaitAsyncLocal()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Assert.Equal("world", AsyncLocal.Value);
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_FlowsExistingAsyncLocal_WhenInvokingAsyncMethodAndSettingAsyncLocalAfterAwait(TaskScheduler scheduler)
        {
            InvokeJobMethodWithFlowOfAsyncLocal(Job.FromExpression(() => CheckAsyncLocalAfterAwait()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static async Task CheckAsyncLocalAfterAwait()
        {
            await Task.Yield();

            Assert.Equal("world", AsyncLocal.Value);
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_FlowsExistingAsyncLocal_WhenInvokingAsyncMethodAndSettingAsyncLocalInTaskContinuation(TaskScheduler scheduler)
        {
            InvokeJobMethodWithFlowOfAsyncLocal(Job.FromExpression(() => CheckAsyncLocalInExplicitTaskContinuation()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static Task CheckAsyncLocalInExplicitTaskContinuation()
        {
            return Task.FromResult(true).ContinueWith(_ =>
            {
                Assert.Equal("world", AsyncLocal.Value);
            });
        }

        private void InvokeJobMethodWithFlowOfAsyncLocal(Job job, TaskScheduler scheduler = null)
        {
            AsyncLocal.Value = "world";

            var parallelism = Math.Max(10, Environment.ProcessorCount);
            Parallel.For(0, parallelism, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, _ =>
            {
                _context.BackgroundJob.Job = job;
                var performer = CreatePerformer(scheduler);

                performer.Perform(_context.Object);

                Assert.Equal("world", AsyncLocal.Value);
            });

            Assert.Equal("world", AsyncLocal.Value);
        }

        [Fact]
        public void Perform_DoesNotLeakAsyncLocal_WhenInvokingSynchronousMethod()
        {
            InvokeJobMethodAndAssertAsyncLocalIsNull(Job.FromExpression(() => SynchronousAsyncLocal()));
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void SynchronousAsyncLocal()
        {
            Assert.Null(AsyncLocal.Value);
            AsyncLocal.Value = "hello";
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_DoesNotLeakAsyncLocal_WhenInvokingSimpleAsyncMethod(TaskScheduler scheduler)
        {
            InvokeJobMethodAndAssertAsyncLocalIsNull(Job.FromExpression(() => SimpleAsyncLocal()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static Task SimpleAsyncLocal()
        {
            Assert.Null(AsyncLocal.Value);
            AsyncLocal.Value = "hello";
            return Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_DoesNotLeakAsyncLocal_WhenInvokingAsyncMethodWithAwait(TaskScheduler scheduler)
        {
            InvokeJobMethodAndAssertAsyncLocalIsNull(Job.FromExpression(() => AsyncAwaitAsyncLocal()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task AsyncAwaitAsyncLocal()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Assert.Null(AsyncLocal.Value);
            AsyncLocal.Value = "hello";
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_DoesNotLeakAsyncLocal_WhenInvokingAsyncMethodAndSettingAsyncLocalAfterAwait(TaskScheduler scheduler)
        {
            InvokeJobMethodAndAssertAsyncLocalIsNull(Job.FromExpression(() => AsyncLocalSetAfterAwait()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static async Task AsyncLocalSetAfterAwait()
        {
            await Task.Yield();

            Assert.Null(AsyncLocal.Value);
            AsyncLocal.Value = "hello";
        }

        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void Perform_DoesNotLeakAsyncLocal_WhenInvokingAsyncMethodAndSettingAsyncLocalInTaskContinuation(TaskScheduler scheduler)
        {
            InvokeJobMethodAndAssertAsyncLocalIsNull(Job.FromExpression(() => AsyncLocalSetInExplicitTaskContinuation()), scheduler);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static Task AsyncLocalSetInExplicitTaskContinuation()
        {
            return Task.FromResult(true).ContinueWith(_ =>
            {
                Assert.Null(AsyncLocal.Value);
                AsyncLocal.Value = "hello";
            });
        }

        private void InvokeJobMethodAndAssertAsyncLocalIsNull(Job job, TaskScheduler scheduler = null)
        {
            var parallelism = Math.Max(10, Environment.ProcessorCount);
            Parallel.For(0, parallelism, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, _ =>
            {
                _context.BackgroundJob.Job = job;
                var performer = CreatePerformer(scheduler);

                performer.Perform(_context.Object);

                Assert.Null(AsyncLocal.Value);
            });

            Assert.Null(AsyncLocal.Value);
        }

        public static IEnumerable<object[]> GetSchedulers()
        {
            yield return new object[] { null };
            yield return new object[] { TaskScheduler.Default };
            yield return new object[] { new Hangfire.Processing.BackgroundTaskScheduler(threadCount: 1) };
        }
#endif

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public void InstanceMethod()
        {
            _methodInvoked = true;
        }

        [UsedImplicitly]
        public class Disposable : IDisposable
        {
            [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
            [SuppressMessage("Performance", "CA1822:Mark members as static")]
            public void Method()
            {
                _methodInvoked = true;
            }

            public void Dispose()
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        [UsedImplicitly]
        public class BrokenDispose : IDisposable
        {
            [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
            [SuppressMessage("Performance", "CA1822:Mark members as static")]
            public void Method()
            {
                _methodInvoked = true;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                throw new InvalidOperationException();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void NullArgumentMethod(string[] argument)
        {
            _methodInvoked = true;
            Assert.Null(argument);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void CancelableJob(IJobCancellationToken token)
        {
            token.ThrowIfCancellationRequested();
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void CancelableJob(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void MethodWithDateTimeArgument(DateTime argument)
        {
            _methodInvoked = true;

            Assert.Equal(SomeDateTime, argument);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void StaticMethod()
        {
            _methodInvoked = true;
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void MethodWithArguments(string stringArg, int intArg)
        {
            _methodInvoked = true;

            Assert.Equal("hello", stringArg);
            Assert.Equal(5, intArg);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void ExceptionMethod()
        {
            throw new InvalidOperationException("exception");
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static async Task TaskExceptionMethod()
        {
            await Task.Yield();

            throw new InvalidOperationException("exception");
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void TaskCanceledExceptionMethod()
        {
            throw new TaskCanceledException();
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static async Task<bool> AsyncMethod(int threadId)
        {
            if (threadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("Start");
            }

            await Task.Yield();

            if (threadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("After Yield");
            }

            await Task.Delay(1).ConfigureAwait(true);

            if (threadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("After Delay");
            }

            Parallel.For(0, 4, new ParallelOptions { MaxDegreeOfParallelism = 4, TaskScheduler = TaskScheduler.Current },
                i =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                });

            if (threadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("After Parallel.For");
            }

            await Task.Delay(1).ConfigureAwait(false);

#if NETCOREAPP1_0
            if (threadId == Thread.CurrentThread.ManagedThreadId)
#else
            if (!Thread.CurrentThread.IsThreadPoolThread)
#endif
            {
                throw new InvalidOperationException("Not running on ThreadPool after ConfigureAwait(false)");
            }

            return true;
        }

        private CoreBackgroundJobPerformer CreatePerformer(TaskScheduler taskScheduler = null)
        {
            return new CoreBackgroundJobPerformer(_activator.Object, taskScheduler);
        }

        private class MyCustomTaskScheduler : TaskScheduler
        {
            public int TasksPerformed { get; private set; }

            protected override void QueueTask(Task task)
            {
                TryExecuteTask(task);
                TasksPerformed++;
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Enumerable.Empty<Task>();
            }
        }
    }
}
