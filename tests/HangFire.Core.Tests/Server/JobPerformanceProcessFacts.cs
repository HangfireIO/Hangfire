using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Server;
using HangFire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class JobPerformanceProcessFacts
    {
        private readonly PerformContext _context;
        private readonly Mock<IJobPerformer> _performer;

        private readonly IList<object> _filters; 

        public JobPerformanceProcessFacts()
        {
            var workerContext = new WorkerContextMock();

            var connection = new Mock<IStorageConnection>();
            const string jobId = "someId";
            var job = Job.FromExpression(() => Method());

            _context = new PerformContext(workerContext.Object, connection.Object, jobId, job);
            _performer = new Mock<IJobPerformer>();

            _filters = new List<object>();
        }

        [Fact]
        public void Run_ThrowsAnException_WhenContextIsNull()
        {
            var process = CreateProcess();

            var exception = Assert.Throws<ArgumentNullException>(
                () => process.Run(null, _performer.Object));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void Run_ThrowsAnException_WhenPerformPerformerIsNull()
        {
            var process = CreateProcess();

            var exception = Assert.Throws<ArgumentNullException>(
                () => process.Run(_context, null));

            Assert.Equal("performer", exception.ParamName);
        }

        [Fact]
        public void Run_CallsPerformStrategy()
        {
            var process = CreateProcess();

            process.Run(_context, _performer.Object);

            _performer.Verify(x => x.Perform(), Times.Once);
        }

        [Fact]
        public void Run_DoesNotCatchExceptions()
        {
            _performer.Setup(x => x.Perform()).Throws<InvalidOperationException>();
            var process = CreateProcess();

            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context, _performer.Object));
        }

        [Fact]
        public void Run_CallsExceptionFilter_OnException()
        {
            var filter = new Mock<IServerExceptionFilter>();
            _filters.Add(filter.Object);

            _performer.Setup(x => x.Perform()).Throws<InvalidOperationException>();
            var process = CreateProcess();

            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context, _performer.Object));

            filter.Verify(x => x.OnServerException(
                It.IsNotNull<ServerExceptionContext>()));
        }

        [Fact, Sequence]
        public void Run_CallsExceptionFilters_InReverseOrder()
        {
            // Arrange
            var filter1 = new Mock<IServerExceptionFilter>();
            var filter2 = new Mock<IServerExceptionFilter>();

            filter2.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>())).InSequence();
            filter1.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>())).InSequence();

            _filters.Add(filter1.Object);
            _filters.Add(filter2.Object);

            _performer.Setup(x => x.Perform()).Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context, _performer.Object));

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_EatsException_WhenItWasHandlerByFilter()
        {
            // Arrange
            _performer.Setup(x => x.Perform()).Throws<InvalidOperationException>();

            var filter = new Mock<IServerExceptionFilter>();
            filter.Setup(x => x.OnServerException(It.IsAny<ServerExceptionContext>()))
                .Callback((ServerExceptionContext x) => x.ExceptionHandled = true);

            _filters.Add(filter.Object);

            var process = CreateProcess();

            // Act & Assert
            Assert.DoesNotThrow(() => process.Run(_context, _performer.Object));
        }

        [Fact, Sequence]
        public void Run_CallsServerFilters_BeforeAndAfterTheCreationOfAJob()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnPerforming(It.IsNotNull<PerformingContext>())).InSequence();
            _performer.Setup(x => x.Perform()).InSequence();
            filter.Setup(x => x.OnPerformed(It.IsNotNull<PerformedContext>())).InSequence();

            var process = CreateProcess();

            // Act
            process.Run(_context, _performer.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact, Sequence]
        public void Run_WrapsFilterCalls_OneIntoAnother()
        {
            // Arrange
            var outerFilter = new Mock<IServerFilter>();
            var innerFilter = new Mock<IServerFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            outerFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>())).InSequence();
            innerFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>())).InSequence();
            innerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>())).InSequence();
            outerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>())).InSequence();

            var process = CreateProcess();

            // Act
            process.Run(_context, _performer.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_DoesNotCallBoth_Perform_And_OnPerforming_WhenFilterCancelsThis()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback((PerformingContext x) => x.Canceled = true);

            var process = CreateProcess();

            // Act
            process.Run(_context, _performer.Object);

            // Assert
            _performer.Verify(x => x.Perform(), Times.Never);
            filter.Verify(x => x.OnPerformed(It.IsAny<PerformedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsOuterFilter_AboutTheCancellationOfCreation()
        {
            // Arrange
            var outerFilter = new Mock<IServerFilter>();
            var innerFilter = new Mock<IServerFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            innerFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback((PerformingContext context) => context.Canceled = true);

            var process = CreateProcess();

            // Act
            process.Run(_context, _performer.Object);

            // Assert
            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Canceled)));
        }

        [Fact]
        public void Run_DoesNotCall_Perform_And_OnPerformed_WhenExceptionOccured_DuringPerformingPhase()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act
            var exception = Assert.Throws<JobPerformanceException>(
                () => process.Run(_context, _performer.Object));

            // Assert
            Assert.IsType<InvalidOperationException>(exception.InnerException);

            _performer.Verify(x => x.Perform(), Times.Never);
            filter.Verify(x => x.OnPerformed(It.IsAny<PerformedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsFiltersAboutException_WhenItIsOccured_DuringThePerformanceOfAJob()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            _filters.Add(filter.Object);

            var exception = new InvalidOperationException();
            _performer.Setup(x => x.Perform()).Throws(exception);

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context, _performer.Object));

            // Assert
            filter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(
                context => context.Exception == exception)));
        }

        [Fact]
        public void Run_TellsOuterFilters_AboutAllExceptions()
        {
            // Arrange
            var outerFilter = new Mock<IServerFilter>();
            var innerFilter = new Mock<IServerFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            var exception = new InvalidOperationException();
            _performer.Setup(x => x.Perform()).Throws(exception);

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context, _performer.Object));

            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception == exception)));
        }

        [Fact]
        public void Run_DoesNotThrow_HandledExceptions()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            _filters.Add(filter.Object);

            var exception = new InvalidOperationException();
            _performer.Setup(x => x.Perform()).Throws(exception);

            filter.Setup(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception == exception)))
                .Callback((PerformedContext x) => x.ExceptionHandled = true);

            var process = CreateProcess();

            // Act & Assert
            Assert.DoesNotThrow(() => process.Run(_context, _performer.Object));
        }

        [Fact]
        public void Run_TellsOuterFilter_EvenAboutHandledException()
        {
            // Arrange
            var outerFilter = new Mock<IServerFilter>();
            var innerFilter = new Mock<IServerFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            _performer.Setup(x => x.Perform()).Throws<InvalidOperationException>();

            innerFilter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Callback((PerformedContext x) => x.ExceptionHandled = true);

            var process = CreateProcess();

            // Act
            Assert.DoesNotThrow(() => process.Run(_context, _performer.Object));

            // Assert
            outerFilter.Verify(x => x.OnPerformed(It.Is<PerformedContext>(context => context.Exception != null)));
        }

        [Fact]
        public void Run_WrapsOnPerformedException_IntoJobPerformanceException()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<InvalidOperationException>();

            _filters.Add(filter.Object);

            var process = CreateProcess();

            // Act & Assert
            var exception = Assert.Throws<JobPerformanceException>(() => 
                process.Run(_context, _performer.Object));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void Run_WrapsOnPerformedException_OccuredAfterAnotherException_IntoJobPerformanceException()
        {
            // Arrange
            var filter = new Mock<IServerFilter>();
            filter.Setup(x => x.OnPerformed(It.IsAny<PerformedContext>()))
                .Throws<InvalidOperationException>();

            _filters.Add(filter.Object);

            _performer.Setup(x => x.Perform()).Throws<ArgumentNullException>();

            var process = CreateProcess();

            // Act & Assert
            var exception = Assert.Throws<JobPerformanceException>(() =>
                process.Run(_context, _performer.Object));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        public static void Method()
        {
        }

        private JobPerformanceProcess CreateProcess()
        {
            return new JobPerformanceProcess(_filters);
        }
    }
}
