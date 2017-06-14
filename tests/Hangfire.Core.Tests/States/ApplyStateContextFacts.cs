using System;
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

        public ApplyStateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _backgroundJob = new BackgroundJobMock();
            _newState = new Mock<IState>();
            _newState.Setup(x => x.Name).Returns(NewState);
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
    }
}
