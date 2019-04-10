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

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Client
{
    public class BackgroundJobFactoryFacts
    {
        private readonly Mock<CreateContext> _context;
        private readonly IList<object> _filters;
        private readonly Mock<IJobFilterProvider> _filterProvider;
        private readonly Mock<IBackgroundJobFactory> _innerFactory;
        private readonly BackgroundJobMock _backgroundJob;

        public BackgroundJobFactoryFacts()
        {
            var storage = new Mock<JobStorage>();
            var connection = new Mock<IStorageConnection>();
            var state = new Mock<IState>();
            _backgroundJob = new BackgroundJobMock();

            _context = new Mock<CreateContext>(storage.Object, connection.Object, _backgroundJob.Job, state.Object)
            {
                CallBase = true
            };
            
            _filters = new List<object>();
            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsNotNull<Job>())).Returns(
                _filters.Select(f => new JobFilter(f, JobFilterScope.Type, null)));
            
            _innerFactory = new Mock<IBackgroundJobFactory>();
            _innerFactory.Setup(x => x.Create(_context.Object)).Returns(_backgroundJob.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProviderIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobFactory(null, _innerFactory.Object));

            Assert.Equal("filterProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInnerFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobFactory(_filterProvider.Object, null));

            Assert.Equal("innerFactory", exception.ParamName);
        }

        [Fact]
        public void Run_ThrowsAnException_WhenContextIsNull()
        {
            var factory = CreateFactory();

            var exception = Assert.Throws<ArgumentNullException>(
                () => factory.Create(null));

            Assert.Equal("context", exception.ParamName);
        }
        
        [Fact]
        public void Run_CallsInnerFactory_ToCreateAJob()
        {
            var factory = CreateFactory();

            factory.Create(_context.Object);

            _innerFactory.Verify(
                x => x.Create(_context.Object), 
                Times.Once);
        }

        [Fact]
        public void Run_ReturnsJobIdentifier()
        {
            var factory = CreateFactory();

            var result = factory.Create(_context.Object);

            Assert.Equal(_backgroundJob.Id, result.Id);
        }

        [Fact]
        public void Run_DoesNotCatchExceptions()
        {
            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var factory = CreateFactory();

            Assert.Throws<InvalidOperationException>(() => factory.Create(_context.Object));
        }

        [Fact]
        public void Run_CallsExceptionFilter_OnException()
        {
            // Arrange
            var filter = new Mock<IClientExceptionFilter>();
            _filters.Add(filter.Object);

            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => factory.Create(_context.Object));
            
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

            _innerFactory
                .Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => factory.Create(_context.Object));

            // Assert - see the `SequenceAttribute` class.
        }

        [Fact]
        public void Run_EatsException_WhenItWasHandlerByFilter_AndReturnsNullJobIdentifier()
        {
            // Arrange
            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var filter = new Mock<IClientExceptionFilter>();
            filter.Setup(x => x.OnClientException(It.IsAny<ClientExceptionContext>()))
                .Callback((ClientExceptionContext x) => x.ExceptionHandled = true);

            _filters.Add(filter.Object);

            var factory = CreateFactory();

            // Act
            var jobId = factory.Create(_context.Object);

            Assert.Null(jobId);
        }

        [Fact, Sequence]
        public void Run_CallsClientFilters_BeforeAndAfterTheCreationOfAJob()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            filter.Setup(x => x.OnCreating(It.IsNotNull<CreatingContext>())).InSequence();

            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .InSequence();

            filter.Setup(x => x.OnCreated(It.IsNotNull<CreatedContext>())).InSequence();

            var factory = CreateFactory();

            // Act
            factory.Create(_context.Object);

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

            var factory = CreateFactory();

            // Act
            factory.Create(_context.Object);

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
            
            var factory = CreateFactory();

            // Act
            var jobId = factory.Create(_context.Object);

            // Assert
            Assert.Null(jobId);

            _innerFactory.Verify(
                x => x.Create(It.IsAny<CreateContext>()), 
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

            var factory = CreateFactory();

            // Act
            factory.Create(_context.Object);

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

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => factory.Create(_context.Object));

            // Assert
            _innerFactory.Verify(
                x => x.Create(It.IsAny<CreateContext>()), 
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
            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws(exception);

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => factory.Create(_context.Object));

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
            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws(exception);

            var factory = CreateFactory();

            // Act
            Assert.Throws<InvalidOperationException>(
                () => factory.Create(_context.Object));

            outerFilter.Verify(x => x.OnCreated(It.Is<CreatedContext>(context => context.Exception == exception)));
        }

        [Fact]
        public void Run_DoesNotThrow_HandledExceptions_AndReturnsNullJobIdentifier()
        {
            // Arrange
            var filter = new Mock<IClientFilter>();
            _filters.Add(filter.Object);

            var exception = new InvalidOperationException();
            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws(exception);

            filter.Setup(x => x.OnCreated(It.Is<CreatedContext>(context => context.Exception == exception)))
                .Callback((CreatedContext x) => x.ExceptionHandled = true);

            var factory = CreateFactory();

            // Act
            var jobId = factory.Create(_context.Object);

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

            _innerFactory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            innerFilter.Setup(x => x.OnCreated(It.IsAny<CreatedContext>()))
                .Callback((CreatedContext x) => x.ExceptionHandled = true);

            var factory = CreateFactory();

            // Act
            factory.Create(_context.Object);

            // Assert
            outerFilter.Verify(x => x.OnCreated(It.Is<CreatedContext>(context => context.Exception != null)));
        }

        public void TestMethod()
        {
        }

        private BackgroundJobFactory CreateFactory()
        {
            return new BackgroundJobFactory(_filterProvider.Object, _innerFactory.Object);
        }
    }
}
