using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.Client
{
    public class DefaultJobCreationProcessFacts
    {
        private const string JobId = "some-job";
        private readonly Mock<CreateContext> _context;
        private readonly IList<object> _filters;
        private readonly Mock<IJobFilterProvider> _filterProvider;
        private readonly Mock<IJobCreationProcess> _innerProcess;

        public DefaultJobCreationProcessFacts()
        {
            var storage = new Mock<JobStorage>();
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<IState>();

            _context = new Mock<CreateContext>(storage.Object, connection.Object, job, state.Object)
            {
                CallBase = true
            };
            
            _filters = new List<object>();
            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsNotNull<Job>())).Returns(
                _filters.Select(f => new JobFilter(f, JobFilterScope.Type, null)));

            _innerProcess = new Mock<IJobCreationProcess>();
            _innerProcess.Setup(x => x.Run((_context.Object))).Returns(JobId);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProviderIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DefaultJobCreationProcess(null, _innerProcess.Object));

            Assert.Equal("filterProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DefaultJobCreationProcess(_filterProvider.Object, null));

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
        public void Run_CallsInnerProcess_ToCreateAJob()
        {
            var process = CreateProcess();

            process.Run(_context.Object);

            _innerProcess.Verify(
                x => x.Run(_context.Object), 
                Times.Once);
        }

        [Fact]
        public void Run_ReturnsJobIdentifier()
        {
            var process = CreateProcess();

            var result = process.Run(_context.Object);

            Assert.Equal(JobId, result);
        }

        [Fact]
        public void Run_DoesNotCatchExceptions()
        {
            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            Assert.Throws<InvalidOperationException>(() => process.Run(_context.Object));
        }

        [Fact]
        public void Run_CallsExceptionFilter_OnException()
        {
            // Arrange
            var filter = new Mock<IClientExceptionFilter>();
            _filters.Add(filter.Object);

            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));
            
            // Assert
            filter.Verify(x => x.OnClientException(
                It.IsNotNull<ClientExceptionContext>()));
        }

        [Fact, Sequence]
        public void Run_CallsExceptionFilters_InReverseOrder()
        {
            // Arrange
            var filter1 = new Mock<IClientExceptionFilter>();
            var filter2 = new Mock<IClientExceptionFilter>();

            filter2.Setup(x => x.OnClientException(It.IsAny<ClientExceptionContext>())).InSequence();
            filter1.Setup(x => x.OnClientException(It.IsAny<ClientExceptionContext>())).InSequence();

            _filters.Add(filter1.Object);
            _filters.Add(filter2.Object);

            _innerProcess
                .Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_EatsException_WhenItWasHandlerByFilter_AndReturnsNullJobIdentifier()
        {
            // Arrange
            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var filter = new Mock<IClientExceptionFilter>();
            filter.Setup(x => x.OnClientException(It.IsAny<ClientExceptionContext>()))
                .Callback((ClientExceptionContext x) => x.ExceptionHandled = true);

            _filters.Add(filter.Object);

            var process = CreateProcess();

            // Act
            var jobId = process.Run(_context.Object);

            Assert.Null(jobId);
        }

        [Fact, Sequence]
        public void Run_CallsClientFilters_BeforeAndAfterTheCreationOfAJob()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnCreating(It.IsNotNull<CreatingContext>())).InSequence();

            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .InSequence();

            filter.Setup(x => x.OnCreated(It.IsNotNull<CreatedContext>())).InSequence();

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact, Sequence]
        public void Run_WrapsFilterCalls_OneIntoAnother()
        {
            // Arrange
            var outerFilter = new Mock<IClientFilter>();
            var innerFilter = new Mock<IClientFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            outerFilter.Setup(x => x.OnCreating(It.IsAny<CreatingContext>())).InSequence();
            innerFilter.Setup(x => x.OnCreating(It.IsAny<CreatingContext>())).InSequence();
            innerFilter.Setup(x => x.OnCreated(It.IsAny<CreatedContext>())).InSequence();
            outerFilter.Setup(x => x.OnCreated(It.IsAny<CreatedContext>())).InSequence();

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_DoesNotCallBoth_CreateJob_And_OnCreated_WhenFilterCancelsThis_AndReturnsNullJobIdentifier()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnCreating(It.IsAny<CreatingContext>()))
                .Callback((CreatingContext x) => x.Canceled = true);
            
            var process = CreateProcess();

            // Act
            var jobId = process.Run(_context.Object);

            // Assert
            Assert.Null(jobId);

            _innerProcess.Verify(
                x => x.Run(It.IsAny<CreateContext>()), 
                Times.Never);

            filter.Verify(x => x.OnCreated(It.IsAny<CreatedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsOuterFilter_AboutTheCancellationOfCreation()
        {
            // Arrange
            var outerFilter = new Mock<IClientFilter>();
            var innerFilter = new Mock<IClientFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            innerFilter.Setup(x => x.OnCreating(It.IsAny<CreatingContext>()))
                .Callback((CreatingContext context) => context.Canceled = true);

            var process = CreateProcess();

            // Act
            process.Run(_context.Object);

            // Assert
            outerFilter.Verify(x => x.OnCreated(It.Is<CreatedContext>(context => context.Canceled)));
        }

        [Fact]
        public void Run_DoesNotCall_CreateJob_And_OnCreated_WhenExceptionOccured_DuringCreatingPhase()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnCreating(It.IsAny<CreatingContext>()))
                .Throws<InvalidOperationException>();

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));

            // Assert
            _innerProcess.Verify(
                x => x.Run(It.IsAny<CreateContext>()), 
                Times.Never);

            filter.Verify(x => x.OnCreated(It.IsAny<CreatedContext>()), Times.Never);
        }

        [Fact]
        public void Run_TellsFiltersAboutException_WhenItIsOccured_DuringTheCreationOfAJob()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            var exception = new InvalidOperationException();
            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws(exception);

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));

            // Assert
            filter.Verify(x => x.OnCreated(It.Is<CreatedContext>(
                context => context.Exception == exception)));
        }

        [Fact]
        public void Run_TellsOuterFilters_AboutAllExceptions()
        {
            // Arrange
            var outerFilter = new Mock<IClientFilter>();
            var innerFilter = new Mock<IClientFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            var exception = new InvalidOperationException();
            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws(exception);

            var process = CreateProcess();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => process.Run(_context.Object));

            outerFilter.Verify(x => x.OnCreated(It.Is<CreatedContext>(context => context.Exception == exception)));
        }

        [Fact]
        public void Run_DoesNotThrow_HandledExceptions_AndReturnsNullJobIdentifier()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            var exception = new InvalidOperationException();
            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws(exception);

            filter.Setup(x => x.OnCreated(It.Is<CreatedContext>(context => context.Exception == exception)))
                .Callback((CreatedContext x) => x.ExceptionHandled = true);

            var process = CreateProcess();

            // Act
            var jobId = process.Run(_context.Object);

            // Assert
            Assert.Null(jobId);
        }

        [Fact]
        public void Run_TellsOuterFilter_EvenAboutHandledException()
        {
            // Arrange
            var outerFilter = new Mock<IClientFilter>();
            var innerFilter = new Mock<IClientFilter>();

            _filters.Add(outerFilter.Object);
            _filters.Add(innerFilter.Object);

            _innerProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            innerFilter.Setup(x => x.OnCreated(It.IsAny<CreatedContext>()))
                .Callback((CreatedContext x) => x.ExceptionHandled = true);

            var process = CreateProcess();

            // Act
            Assert.DoesNotThrow(() => process.Run(_context.Object));

            // Assert
            outerFilter.Verify(x => x.OnCreated(It.Is<CreatedContext>(context => context.Exception != null)));
        }

        public void TestMethod()
        {
        }

        private DefaultJobCreationProcess CreateProcess()
        {
            return new DefaultJobCreationProcess(_filterProvider.Object, _innerProcess.Object);
        }
    }
}
