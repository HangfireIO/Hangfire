using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.Server;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class JobPerformanceProcessFacts
    {
        private readonly PerformContextMock _context;
        private readonly Mock<IJobPerformanceProcess> _innerProcess;
        private readonly IList<object> _filters;
        private readonly Mock<IJobFilterProvider> _filterProvider;

        public JobPerformanceProcessFacts()
        {
            _context = new PerformContextMock();
            _innerProcess = new Mock<IJobPerformanceProcess>();

            _filters = new List<object>();
            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsNotNull<Job>())).Returns(
                _filters.Select(f => new JobFilter(f, JobFilterScope.Type, null)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProvider_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobPerformanceProcess(null, _innerProcess.Object));

            Assert.Equal("filterProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerProcess_IsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new JobPerformanceProcess(_filterProvider.Object, null));

            Assert.Equal("innerProcess", exception.ParamName);
        }

        [Fact]
        public void Run_ThrowsAnException_WhenContextIsNull()
        {
            var process = CreateProcess();

            var exception = Assert.Throws<ArgumentNullException>(
                () => process.Run(null));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Run_CallsTheRunMethod_OfInnerProcess()
        {
            var process = CreateProcess();

            process.Run(_context.Object);

            _innerProcess.Verify(x => x.Run(_context.Object), Times.Once);
        }

        [Fact]
        public void Run_StoresJobReturnValueInPerformedContext()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            var process = CreateProcess();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Returns("Returned value");

            // Act
            process.Run(_context.Object);

            // Assert
            filter.Verify(
                x => x.OnPerformed(It.Is<PerformedContext>(context => (string)context.Result == "Returned value")));
        }

        [Fact]
        public void Run_ReturnsValueReturnedByJob()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            var process = CreateProcess();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Returns("Returned value");

            // Act
            var result = process.Run(_context.Object);

            // Assert
            Assert.Equal("Returned value", result);
        }

        [Fact]
        public void Run_DoesNotCatchExceptions()
        {
            // Arrange
            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => process.Run(_context.Object));
        }

        [Fact]
        public void Run_CallsExceptionFilter_OnException()
        {
            // Arrange
            var filter = CreateFilter<IServerExceptionFilter>();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<InvalidOperationException>();
            
            var process = CreateProcess();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => process.Run(_context.Object));

            filter.Verify(x => x.OnServerException(
                It.IsNotNull<ServerExceptionContext>()));
        }

        [Fact, Sequence]
        public void Run_CallsExceptionFilters_InReverseOrder()
        {
            // Arrange
            var filter1 = CreateFilter<IServerExceptionFilter>();
            var filter2 = CreateFilter<IServerExceptionFilter>();

            filter2.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>())).InSequence();
            filter1.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>())).InSequence();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(() => process.Run(_context.Object));

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_EatsException_WhenItWasHandlerByFilter()
        {
            // Arrange
            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<InvalidOperationException>();

            var filter = CreateFilter<IServerExceptionFilter>();
            filter.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>()))
                .Callback((ServerExceptionContext x) => x.ExceptionHandled = true);
            
            var process = CreateProcess();

            // Act & Assert
            Assert.DoesNotThrow(() => process.Run(_context.Object));
        }

        [Fact, Sequence]
        public void Run_CallsServerFilters_BeforeAndAfterTheCreationOfAJob()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            filter.Setup(x => x.OnPerforming(It.IsNotNull<PerformingContext>()))
                .InSequence();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .InSequence();

            filter.Setup(x => x.OnPerformed(It.IsNotNull<PerformedContext>()))
                .InSequence();

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

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

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_DoesNotCallBoth_Perform_And_OnPerforming_WhenFilterCancelsThis()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback((PerformingContext x) => x.Canceled = true);

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert
            _innerProcess.Verify(x => x.Run(_context.Object), Times.Never);

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

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

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

            var process = CreateProcess();

            // Act
            var exception = Assert.Throws<JobPerformanceException>(
                () => process.Run(_context.Object));

            // Assert
            Assert.IsType<InvalidOperationException>(exception.InnerException);

            _innerProcess.Verify(x => x.Run(It.IsAny<PerformContext>()), Times.Never);

            filter.Verify(x => x.OnPerformed(It.IsAny<PerformedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsFiltersAboutException_WhenItIsOccured_DuringThePerformanceOfAJob()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            var exception = new InvalidOperationException();
            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws(exception);

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(() => process.Run(_context.Object));

            // Assert
            filter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(
                context => context.Exception == exception)));
        }

        [Fact]
        public void Run_TellsOuterFilters_AboutAllExceptions()
        {
            // Arrange
            var outerFilter = CreateFilter<IServerFilter>();
            var innerFilter = CreateFilter<IServerFilter>();

            var exception = new InvalidOperationException();
            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws(exception);

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(() => process.Run(_context.Object));

            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception == exception)));
        }

        [Fact]
        public void Run_DoesNotThrow_HandledExceptions()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();

            var exception = new InvalidOperationException();
            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws(exception);

            filter.Setup(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception == exception)))
                .Callback((PerformedContext x) => x.ExceptionHandled = true);

            var process = CreateProcess();

            // Act & Assert
            Assert.DoesNotThrow(() => process.Run(_context.Object));
        }

        [Fact]
        public void Run_TellsOuterFilter_EvenAboutHandledException()
        {
            // Arrange
            var outerFilter = CreateFilter<IServerFilter>();
            var innerFilter = CreateFilter<IServerFilter>();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<InvalidOperationException>();

            innerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Callback((PerformedContext x) => x.ExceptionHandled = true);

            var process = CreateProcess();

            // Act
            Assert.DoesNotThrow(() => process.Run(_context.Object));

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

            var process = CreateProcess();

            // Act & Assert
            var exception = Assert.Throws<JobPerformanceException>(() => 
                process.Run(_context.Object));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void Run_WrapsOnPerformedException_OccuredAfterAnotherException_IntoJobPerformanceException()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<InvalidOperationException>();

            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<ArgumentNullException>();

            var process = CreateProcess();

            // Act & Assert
            var exception = Assert.Throws<JobPerformanceException>(() =>
                process.Run(_context.Object));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void Run_ServerFiltersAreNotInvoked_OnOperationCanceledException()
        {
            // Arrange
            _innerProcess
                .Setup(x => x.Run(_context.Object))
                .Throws<OperationCanceledException>();

            var filter = CreateFilter<IServerExceptionFilter>();
            var process = CreateProcess();

            // Act
            Assert.Throws<OperationCanceledException>(
                () => process.Run(_context.Object));

            // Assert
            filter.Verify(
                x => x.OnServerException(It.IsAny<ServerExceptionContext>()),
                Times.Never);
        }

        [Fact]
        public void Run_ThrowsOperationCanceledException_OccurredInPreFilterMethods()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Throws<OperationCanceledException>();

            var process = CreateProcess();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => process.Run(_context.Object));
        }

        [Fact]
        public void Run_ThrowsOperationCanceledException_OccurredInPostFilterMethods()
        {
            // Arrange
            var filter = CreateFilter<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<OperationCanceledException>();

            var process = CreateProcess();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(
                () => process.Run(_context.Object));
        }

        public static void Method()
        {
        }

        private JobPerformanceProcess CreateProcess()
        {
            return new JobPerformanceProcess(_filterProvider.Object, _innerProcess.Object);
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
