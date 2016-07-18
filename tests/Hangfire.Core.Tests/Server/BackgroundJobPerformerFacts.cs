using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Moq;
using Moq.Sequences;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundJobPerformerFacts
    {
        private readonly PerformContextMock _context;
        private readonly Mock<IBackgroundJobPerformer> _innerPerformer;
        private readonly IList<object> _filters;
        private readonly Mock<IJobFilterProvider> _filterProvider;

        public BackgroundJobPerformerFacts()
        {
            _context = new PerformContextMock();
            _innerPerformer = new Mock<IBackgroundJobPerformer>();

            _filters = new List<object>();
            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsNotNull<Job>())).Returns(
                _filters.Select(f => new JobFilter(f, JobFilterScope.Type, null)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProvider_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobPerformer(null, _innerPerformer.Object));

            Assert.Equal("filterProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerPerformer_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobPerformer(_filterProvider.Object, (IBackgroundJobPerformer)null));

            Assert.Equal("innerPerformer", exception.ParamName);
        }

        [Fact]
        public void Run_ThrowsAnException_WhenContextIsNull()
        {
            var performer = CreatePerformer();

            var exception = Assert.Throws<ArgumentNullException>(
                () => performer.Perform(null));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Run_CallsTheRunMethod_OfInnerProcess()
        {
            var performer = CreatePerformer();

            performer.Perform(_context.Object);

            _innerPerformer.Verify(x => x.Perform(_context.Object), Times.Once);
        }

        [Fact]
        public void Run_StoresJobReturnValueInPerformedContext()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            var performer = CreatePerformer();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Returns("Returned value");

            // Act
            performer.Perform(_context.Object);

            // Assert
            filter.Verify(
                x => x.OnPerformed(It.Is<PerformedContext>(context => (string)context.Result == "Returned value")));
        }

        [Fact]
        public void Run_ReturnsValueReturnedByJob()
        {
            // Arrange
            // ReSharper disable once UnusedVariable
            var filter = CreateFilter<IServerFilter>();
            var performer = CreatePerformer();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Returns("Returned value");

            // Act
            var result = performer.Perform(_context.Object);

            // Assert
            Assert.Equal("Returned value", result);
        }

        [Fact]
        public void Run_DoesNotCatchExceptions()
        {
            // Arrange
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<InvalidOperationException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_CallsExceptionFilter_OnException()
        {
            // Arrange
            var filter = CreateFilter<IServerExceptionFilter>();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<InvalidOperationException>();
            
            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => performer.Perform(_context.Object));

            filter.Verify(x => x.OnServerException(It.Is<ServerExceptionContext>(context =>
                context.Exception is InvalidOperationException)));
        }

        [Fact, Sequence]
        public void Run_CallsExceptionFilters_InReverseOrder()
        {
            // Arrange
            var filter1 = CreateFilter<IServerExceptionFilter>();
            var filter2 = CreateFilter<IServerExceptionFilter>();

            filter2.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>())).InSequence();
            filter1.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>())).InSequence();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<InvalidOperationException>();

            var performer = CreatePerformer();

            // Act
            Assert.Throws<InvalidOperationException>(() => performer.Perform(_context.Object));

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_EatsException_WhenItWasHandlerByFilter()
        {
            // Arrange
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<InvalidOperationException>();

            var filter = CreateFilter<IServerExceptionFilter>();
            filter.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>()))
                .Callback((ServerExceptionContext x) => x.ExceptionHandled = true);
            
            var performer = CreatePerformer();

            // Act & Assert does not throw
            performer.Perform(_context.Object);
        }

        [Fact, Sequence]
        public void Run_CallsServerFilters_BeforeAndAfterTheCreationOfAJob()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            filter.Setup(x => x.OnPerforming(It.IsNotNull<PerformingContext>()))
                .InSequence();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .InSequence();

            filter.Setup(x => x.OnPerformed(It.IsNotNull<PerformedContext>()))
                .InSequence();

            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact, Sequence]
        public void Run_WrapsFilterCalls_OneIntoAnother()
        {
            // Arrange
            var outerFilter = CreateFilter<IServerFilter>();
            var innerFilter = CreateFilter<IServerFilter>();

            outerFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>())).InSequence();
            innerFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>())).InSequence();
            innerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>())).InSequence();
            outerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>())).InSequence();

            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_DoesNotCallBoth_Perform_And_OnPerforming_WhenFilterCancelsThis()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback((PerformingContext x) => x.Canceled = true);

            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert
            _innerPerformer.Verify(x => x.Perform(_context.Object), Times.Never);

            filter.Verify(x => x.OnPerformed(It.IsAny<PerformedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsOuterFilter_AboutTheCancellationOfCreation()
        {
            // Arrange
            var outerFilter = CreateFilter<IServerFilter>();
            var innerFilter = CreateFilter<IServerFilter>();

            innerFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback((PerformingContext context) => context.Canceled = true);

            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert
            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Canceled)));
        }

        [Fact]
        public void Run_DoesNotCall_Perform_And_OnPerformed_WhenExceptionOccured_DuringPerformingPhase()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Throws<InvalidOperationException>();

            var performer = CreatePerformer();

            // Act
            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            // Assert
            Assert.IsType<InvalidOperationException>(exception.InnerException);

            _innerPerformer.Verify(x => x.Perform(It.IsAny<PerformContext>()), Times.Never);

            filter.Verify(x => x.OnPerformed(It.IsAny<PerformedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsFiltersAboutException_WhenItIsOccured_DuringThePerformanceOfAJob()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            var exception = new InvalidOperationException();
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws(exception);

            var performer = CreatePerformer();

            // Act
            Assert.Throws<InvalidOperationException>(() => performer.Perform(_context.Object));

            // Assert
            filter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(
                context => context.Exception == exception)));
        }

        [Fact]
        public void Run_TellsOuterFilters_AboutAllExceptions()
        {
            // Arrange
            var outerFilter = CreateFilter<IServerFilter>();
            // ReSharper disable once UnusedVariable
            var innerFilter = CreateFilter<IServerFilter>();

            var exception = new InvalidOperationException();
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws(exception);

            var performer = CreatePerformer();

            // Act
            Assert.Throws<InvalidOperationException>(() => performer.Perform(_context.Object));

            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception == exception)));
        }

        [Fact]
        public void Run_DoesNotThrow_HandledExceptions()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            var exception = new InvalidOperationException();
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws(exception);

            filter.Setup(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception == exception)))
                .Callback((PerformedContext x) => x.ExceptionHandled = true);

            var performer = CreatePerformer();

            // Act & Assert does not throw
            performer.Perform(_context.Object);
        }

        [Fact]
        public void Run_TellsOuterFilter_EvenAboutHandledException()
        {
            // Arrange
            var outerFilter = CreateFilter<IServerFilter>();
            var innerFilter = CreateFilter<IServerFilter>();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<InvalidOperationException>();

            innerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Callback((PerformedContext x) => x.ExceptionHandled = true);

            var performer = CreatePerformer();

            // Act
            performer.Perform(_context.Object);

            // Assert
            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception != null)));
        }

        [Fact]
        public void Run_WrapsOnPerformedException_IntoJobPerformanceException()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<InvalidOperationException>();

            var performer = CreatePerformer();

            // Act & Assert
            var exception = Assert.Throws<JobPerformanceException>(() => 
                performer.Perform(_context.Object));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void Run_WrapsOnPerformedException_OccuredAfterAnotherException_IntoJobPerformanceException()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<InvalidOperationException>();

            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<ArgumentNullException>();

            var performer = CreatePerformer();

            // Act & Assert
            var exception = Assert.Throws<JobPerformanceException>(() =>
                performer.Perform(_context.Object));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void Run_ExceptionFiltersAreNOTInvoked_OnJobAbortedException()
        {
            // Arrange
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<JobAbortedException>();

            var filter = CreateFilter<IServerExceptionFilter>();
            var performer = CreatePerformer();

            // Act
            Assert.Throws<JobAbortedException>(() => performer.Perform(_context.Object));

            // Assert
            filter.Verify(
                x => x.OnServerException(It.IsAny<ServerExceptionContext>()),
                Times.Never);
        }

        [Fact]
        public void Run_ExceptionFiltersAreNOTInvoked_OnOperationCanceledException_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(cts.Token);
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<OperationCanceledException>();

            var filter = CreateFilter<IServerExceptionFilter>();
            var performer = CreatePerformer();

            // Act
            Assert.Throws<OperationCanceledException>(
                () => performer.Perform(_context.Object));

            // Assert
            filter.Verify(
                x => x.OnServerException(It.IsAny<ServerExceptionContext>()),
                Times.Never);
        }

        [Fact]
        public void Run_ExceptionFiltersAreInvoked_OnOperationCanceledException_WhenShutdownTokenIsNOTCanceled()
        {
            // Arrange
            _innerPerformer
                .Setup(x => x.Perform(_context.Object))
                .Throws<OperationCanceledException>();

            var filter = CreateFilter<IServerExceptionFilter>();
            var performer = CreatePerformer();

            // Act
            Assert.Throws<OperationCanceledException>(
                () => performer.Perform(_context.Object));

            // Assert
            filter.Verify(
                x => x.OnServerException(It.IsAny<ServerExceptionContext>()),
                Times.Once);
        }

        [Fact]
        public void Run_ThrowsOperationCanceledException_OccurredInPreFilterMethods_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(cts.Token);
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Throws<OperationCanceledException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_ThrowsJobPerformanceException_InsteadOfOperationCanceled_OccurredInPreFilterMethods_WhenShutdownTokenIsNotCanceled()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Throws<OperationCanceledException>();

            var performer = CreatePerformer();

            // Act
            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            // Assert
            Assert.IsType<OperationCanceledException>(exception.InnerException);
        }

        [Fact]
        public void Run_ThrowsOperationCanceledException_OccurredInPostFilterMethods_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _context.CancellationToken.Setup(x => x.ShutdownToken).Returns(cts.Token);
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<OperationCanceledException>();

            var performer = CreatePerformer();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() => performer.Perform(_context.Object));
        }

        [Fact]
        public void Run_ThrowsJobPerformanceException_InsteadOfOperationCanceled_OccurredInPostFilterMethods_WhenShutdownTokenIsNOTCanceled()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<OperationCanceledException>();

            var performer = CreatePerformer();

            // Act
            var exception = Assert.Throws<JobPerformanceException>(
                () => performer.Perform(_context.Object));

            // Assert
            Assert.IsType<OperationCanceledException>(exception.InnerException);
        }

        public static void Method()
        {
        }

        private BackgroundJobPerformer CreatePerformer()
        {
            return new BackgroundJobPerformer(_filterProvider.Object, _innerPerformer.Object);
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
