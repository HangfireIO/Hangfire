using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ElectStateContextFacts
    {
        private readonly Mock<IState> _candidateState;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<JobStorage> _storage;
        private readonly BackgroundJobMock _backgroundJob;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public ElectStateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _backgroundJob = new BackgroundJobMock();
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _candidateState = new Mock<IState>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () =>
                    new ElectStateContext(null, _connection.Object, _transaction.Object, _backgroundJob.Object, _candidateState.Object, null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _storage.Object,
                    null,
                    _transaction.Object,
                    _backgroundJob.Object,
                    _candidateState.Object,
                    null));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTransactionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _storage.Object,
                    _connection.Object,
                    null,
                    _backgroundJob.Object,
                    _candidateState.Object,
                    null));

            Assert.Equal("transaction", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenBackgroundJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(_storage.Object, _connection.Object, _transaction.Object, null, _candidateState.Object, null));

            Assert.Equal("backgroundJob", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCandidateStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _storage.Object,
                    _connection.Object,
                    _transaction.Object,
                    _backgroundJob.Object,
                    null,
                    null));

            Assert.Equal("candidateState", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetAllProperties()
        {
            var context = CreateContext();
            
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_transaction.Object, context.Transaction);
            Assert.Same(_backgroundJob.Object, context.BackgroundJob);
            Assert.Same(_candidateState.Object, context.CandidateState);
            Assert.Equal("State", context.CurrentState);
            Assert.Empty(context.TraversedStates);
        }

        [Fact]
        public void SetCandidateState_ThrowsAnException_WhenValueIsNull()
        {
            var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => context.CandidateState = null);
        }

        [Fact]
        public void SetCandidateState_SetsTheGivenValue()
        {
            var context = CreateContext();
            var newState = new Mock<IState>();

            context.CandidateState = newState.Object;

            Assert.Same(newState.Object, context.CandidateState);
        }

        [Fact]
        public void SetCandidateState_AddsPreviousCandidateState_ToTraversedStatesList()
        {
            var context = CreateContext();
            var state = new Mock<IState>();

            context.CandidateState = state.Object;

            Assert.Contains(_candidateState.Object, context.TraversedStates);
        }

        [Fact]
        public void SetJobParameter_CallsTheCorrespondingMethod_WithJsonEncodedValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", "Value");

            _connection.Verify(x => x.SetJobParameter(
                _backgroundJob.Id, "Name", JobHelper.ToJson("Value")));
        }

        [Fact]
        public void SetJobParameter_CanReceiveNullValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", (string)null);

            _connection.Verify(x => x.SetJobParameter(
                _backgroundJob.Id, "Name", JobHelper.ToJson(null)));
        }

        [Fact]
        public void GetJobParameter_CallsTheCorrespondingMethod_WithJsonDecodedValue()
        {
            var context = CreateContext();
            _connection.Setup(x => x.GetJobParameter(_backgroundJob.Id, "Name"))
                .Returns(JobHelper.ToJson("Value"));

            var value = context.GetJobParameter<string>("Name");

            Assert.Equal("Value", value);
        }

        [Fact]
        public void GetJobParameter_ReturnsDefaultValue_WhenNoValueProvided()
        {
            var context = CreateContext();
            _connection.Setup(x => x.GetJobParameter("1", "Value"))
                .Returns(JobHelper.ToJson(null));

            var value = context.GetJobParameter<int>("Name");

            Assert.Equal(default(int), value);
        }

        private ElectStateContext CreateContext()
        {
            return new ElectStateContext(
                _storage.Object,
                _connection.Object,
                _transaction.Object,
                _backgroundJob.Object,
                _candidateState.Object,
                "State");
        }
    }
}
