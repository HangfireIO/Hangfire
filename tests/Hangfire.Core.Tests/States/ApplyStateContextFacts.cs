using System;
using System.Collections.Generic;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.States
{
    public class ApplyStateContextFacts
    {
        private const string OldState = "SomeState";
        private const string NewState = "NewState";

        private readonly Mock<IState> _newState;
        private readonly Mock<JobStorage> _storage;
        private readonly BackgroundJobMock _backgroundJob;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IProfiler> _profiler;
        private readonly Mock<IReadOnlyDictionary<string, object>> _customData;

        public ApplyStateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _backgroundJob = new BackgroundJobMock();
            _newState = new Mock<IState>();
            _newState.Setup(x => x.Name).Returns(NewState);
            _profiler = new Mock<IProfiler>();
            _customData = new Mock<IReadOnlyDictionary<string, object>>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(null, _connection.Object, _transaction.Object, _backgroundJob.Object, _newState.Object, OldState));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () =>
                    new ApplyStateContext(_storage.Object, null, _transaction.Object, _backgroundJob.Object,
                        _newState.Object, OldState));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTransactionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () =>
                    new ApplyStateContext(_storage.Object, _connection.Object, null, _backgroundJob.Object, _newState.Object, OldState));

            Assert.Equal("transaction", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenBackgroundJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_storage.Object, _connection.Object, _transaction.Object, null, _newState.Object, OldState));

            Assert.Equal("backgroundJob", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(_storage.Object, _connection.Object, _transaction.Object, _backgroundJob.Object, null, OldState));

            Assert.Equal("newState", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProfilerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ApplyStateContext(
                    _storage.Object,
                    _connection.Object,
                    _transaction.Object,
                    _backgroundJob.Object,
                    _newState.Object,
                    OldState,
                    null));

            Assert.Equal("profiler", exception.ParamName);
        }

        [Fact]
        public void Ctor_ShouldSetPropertiesCorrectly()
        {
            var context = new ApplyStateContext(
                _storage.Object,
                _connection.Object,
                _transaction.Object,
                _backgroundJob.Object,
                _newState.Object,
                OldState);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_transaction.Object, context.Transaction);
            Assert.Same(_backgroundJob.Object, context.BackgroundJob);
            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Equal(_storage.Object.JobExpirationTimeout, context.JobExpirationTimeout);
        }

        [Fact]
        public void InternalCtor_CorrectlySetsAllTheProperties()
        {
            var context = new ApplyStateContext(
                _storage.Object,
                _connection.Object,
                _transaction.Object,
                _backgroundJob.Object,
                _newState.Object,
                OldState,
                _profiler.Object,
                _customData.Object);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_transaction.Object, context.Transaction);
            Assert.Same(_backgroundJob.Object, context.BackgroundJob);
            Assert.Equal(OldState, context.OldStateName);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Equal(_storage.Object.JobExpirationTimeout, context.JobExpirationTimeout);
            Assert.Same(_customData.Object, context.CustomData);
        }

        [Fact]
        public void CopyCtor_ForElectStateContext_CorrectlySetsAllTheProperties()
        {
            var electContext = new ElectStateContextMock();
            var context = new ApplyStateContext(_transaction.Object, electContext.Object);

            Assert.Same(electContext.Object.Storage, context.Storage);
            Assert.Same(electContext.Object.Connection, context.Connection);
            Assert.Same(_transaction.Object, context.Transaction);
            Assert.Same(electContext.Object.BackgroundJob, context.BackgroundJob);
            Assert.Equal(electContext.Object.CurrentState, context.OldStateName);
            Assert.Same(electContext.Object.CandidateState, context.NewState);
            Assert.Null(context.CustomData);
        }

        [Fact]
        public void CopyCtor_ForElectStateContext_CopiesCustomData_ToAnotherDictionary()
        {
            // Arrange
            var dictionary = new Dictionary<string, object> { { "lalala", new object() } };
            var electContext = new ElectStateContextMock { ApplyContext = { CustomData = dictionary } };

            // Act
            var context = new ApplyStateContext(_transaction.Object, electContext.Object);

            // Assert
            Assert.Equal(electContext.Object.CustomData, context.CustomData);
            Assert.NotSame(electContext.Object.CustomData, context.CustomData);
        }
    }
}
