using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Core.Tests.Common;
using Hangfire.Server;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class CoreBackgroundJobPerformerFacts : IDisposable
    {
        private readonly Mock<JobActivator> _activator;
        private readonly PerformContextMock _context;

        private readonly IList<object> _filters;
        private readonly Mock<IJobFilterProvider> _filterProvider;

        private static readonly DateTime SomeDateTime = new DateTime(2014, 5, 30, 12, 0, 0);
        private static bool _methodInvoked;
        private static bool _disposed;

        public CoreBackgroundJobPerformerFacts()
        {
            _activator = new Mock<JobActivator>() { CallBase = true };
            _context = new PerformContextMock();

            _filters = new List<object>();
            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsNotNull<Job>())).Returns(
                _filters.Select(f => new JobFilter(f, JobFilterScope.Type, null)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenActivatorIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new CoreBackgroundJobPerformer(null, _filterProvider.Object));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProviderIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new CoreBackgroundJobPerformer(_activator.Object, null));

            Assert.Equal("filterProvider", exception.ParamName);
        }

        [Fact, StaticLock]
        public void Run_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression(() => StaticMethod());
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            _context.BackgroundJob.Job = Job.FromExpression<Disposable>(x => x.Method());
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Run_PassesArguments_ToACallingMethod()
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

        [Fact, StaticLock]
        public void Run_PassesCorrectDateTime_IfItWasSerialized_UsingTypeConverter()
        {
            // Arrange
            _methodInvoked = false;
            var typeConverter = TypeDescriptor.GetConverter(typeof(DateTime));
            var convertedDate = typeConverter.ConvertToInvariantString(SomeDateTime);

            var type = typeof(CoreBackgroundJobPerformerFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            _context.BackgroundJob.Job = new Job(type, method, new[] { convertedDate });
            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_PassesCorrectDateTime_IfItWasSerialized_UsingOldFormat()
        {
            // Arrange
            _methodInvoked = false;
            var convertedDate = SomeDateTime.ToString("MM/dd/yyyy HH:mm:ss.ffff");

            var type = typeof(CoreBackgroundJobPerformerFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            _context.BackgroundJob.Job = new Job(type, method, new[] { convertedDate });
            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Run_PassesCorrectDateTimeArguments()
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
        public void Run_WorksCorrectly_WithNullValues()
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
        public void Run_ThrowsException_WhenActivatorThrowsAnException()
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
        public void Run_ThrowsPerformanceException_WhenActivatorReturnsNull()
        {
            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Returns(null);
            _context.BackgroundJob.Job = Job.FromExpression(() => InstanceMethod());
            var performer = CreatePerformer();

            Assert.Throws<InvalidOperationException>(
                () => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_ThrowsPerformanceException_OnArgumentsDeserializationFailure()
        {
            var type = typeof(JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");
            _context.BackgroundJob.Job = new Job(type, method, new object[] { "sdfa" });
            var performer = CreatePerformer();

            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.NotNull(exception.InnerException);
        }

        [Fact, StaticLock]
        public void Run_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;
            _context.BackgroundJob.Job = Job.FromExpression<BrokenDispose>(x => x.Method());
            var performer = CreatePerformer();

            Assert.Throws<InvalidOperationException>(
                () => performer.Perform(_context.Object));

            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Run_ThrowsPerformanceException_WithUnwrappedInnerException()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => ExceptionMethod());
            var performer = CreatePerformer();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }

        [Fact]
        public void Run_ThrowsPerformanceException_WhenMethodThrownTaskCanceledException()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => TaskCanceledExceptionMethod());
            var performer = CreatePerformer();

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            Assert.IsType<TaskCanceledException>(thrownException.InnerException);
        }

        [Fact]
        public void Run_PassesCancellationToken_IfThereIsIJobCancellationTokenParameter()
        {
            // Arrange
            _context.BackgroundJob.Job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _context.CancellationToken.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();
            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_ReturnsValue_WhenCallingFunctionReturningValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression<JobFacts.Instance>(x => x.FunctionReturningValue());
            var performer = CreatePerformer();

            var result = performer.Perform(_context.Object);

            Assert.Equal("Return value", result);
        }

        [Fact, StaticLock, Sequence]
        public void Run_CallsActivationFilters_BeforeAndAfterJobActivation()
        {
            var filter = CreateFilter<IActivationFilter>();
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());

            filter.Setup(x => x.OnActivating(It.IsNotNull<ActivatingContext>()))
                .InSequence();

            _activator.Setup(x => x.ActivateJob(_context.BackgroundJob.Job.Type))
                .InSequence()
                .Returns(new CoreBackgroundJobPerformerFacts());

            filter.Setup(x => x.OnActivated(It.IsNotNull<ActivatedContext>()))
                .InSequence();

            var performer = CreatePerformer();
            performer.Perform(_context.Object);
        }

        [Fact, StaticLock]
        public void Run_CallsOnActivated_WithCreatedJobInstance()
        {
            var filter = CreateFilter<IActivationFilter>();
            var createdInstance = new CoreBackgroundJobPerformerFacts();
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());

            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>()))
                .Returns(createdInstance);

            var performer = CreatePerformer();
            performer.Perform(_context.Object);

            filter.Verify(x => x.OnActivated(It.Is<ActivatedContext>(context => context.Instance == createdInstance)));
        }

        [Fact]
        public void Run_CallsOnActivated_WithActivationException()
        {
            var filter = CreateFilter<IActivationFilter>();
            var exception = new Exception();
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());

            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Throws(exception);

            var performer = CreatePerformer();
            var thrownException = Assert.Throws<Exception>(
                () => performer.Perform(_context.Object));

            filter.Verify(x => x.OnActivated(It.Is<ActivatedContext>(context => context.Exception == exception)));
            Assert.Equal(exception, thrownException);
        }

        [Fact, StaticLock, Sequence]
        public void Run_InvokesAllActivationFilters()
        {
            var filter1 = CreateFilter<IActivationFilter>();
            var filter2 = CreateFilter<IActivationFilter>();
            _context.BackgroundJob.Job = Job.FromExpression<CoreBackgroundJobPerformerFacts>(x => x.InstanceMethod());

            filter1.Setup(x => x.OnActivating(It.IsNotNull<ActivatingContext>())).InSequence();
            filter2.Setup(x => x.OnActivating(It.IsNotNull<ActivatingContext>())).InSequence();
            filter1.Setup(x => x.OnActivated(It.IsNotNull<ActivatedContext>())).InSequence();
            filter2.Setup(x => x.OnActivated(It.IsNotNull<ActivatedContext>())).InSequence();

            var performer = CreatePerformer();
            performer.Perform(_context.Object);
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

        public static void TaskCanceledExceptionMethod()
        {
            throw new TaskCanceledException();
        }

        private CoreBackgroundJobPerformer CreatePerformer()
        {
            return new CoreBackgroundJobPerformer(_activator.Object, _filterProvider.Object);
        }

        private Mock<T> CreateFilter<T>()
            where T : class
        {
            var filter = new Mock<T>();
            _filters.Add(filter.Object);

            return filter;
        }
    }
}
