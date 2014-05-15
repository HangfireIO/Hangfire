using System;
using System.Linq;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ElectStateContextFacts
    {
        private const string JobId = "1";
        private readonly StateContextMock _stateContext;
        private readonly Mock<IState> _candidateState;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public ElectStateContextFacts()
        {
            _connection = new Mock<IStorageConnection>();

            _stateContext = new StateContextMock();
            _stateContext.JobIdValue = JobId;
            _stateContext.ConnectionValue = _connection;

            _candidateState = new Mock<IState>();
            
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCandidateStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _stateContext.Object,
                    null,
                    null));

            Assert.Equal("candidateState", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetAllProperties()
        {
            var context = CreateContext();

            Assert.Equal(_stateContext.Object.JobId, context.JobId);
            Assert.Equal(_stateContext.Object.Job, context.Job);

            Assert.Same(_candidateState.Object, context.CandidateState);
            Assert.Equal("State", context.CurrentState);
            Assert.Same(_connection.Object, context.Connection);
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
        public void SetJobParameter_CallsTheCorrespondingMethod_WithJsonEncodedValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", "Value");

            _connection.Verify(x => x.SetJobParameter(
                JobId, "Name", JobHelper.ToJson("Value")));
        }

        [Fact]
        public void SetJobParameter_CanReceiveNullValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", (string)null);

            _connection.Verify(x => x.SetJobParameter(
                JobId, "Name", JobHelper.ToJson(null)));
        }

        [Fact]
        public void GetJobParameter_CallsTheCorrespondingMethod_WithJsonDecodedValue()
        {
            var context = CreateContext();
            _connection.Setup(x => x.GetJobParameter("1", "Name"))
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

        [Fact]
        public void ElectState_ThrowsAnException_WhenFiltersArrayIsNull()
        {
            var context = CreateContext();

            Assert.Throws<ArgumentNullException>(() => context.ElectState(null));
        }

        [Fact]
        public void ElectState_ReturnsCandidateState_WhenFiltersArrayIsEmpty()
        {
            var context = CreateContext();

            var electedState = context.ElectState(Enumerable.Empty<IElectStateFilter>());

            Assert.Same(_candidateState.Object, electedState);
            _connection.Verify(x => x.CreateWriteTransaction(), Times.Never);
        }

        [Fact]
        public void ElectState_AddsJobHistory_WhenAFilterChangesCandidateState()
        {
            // Arrange
            var newState = new Mock<IState>();

            var filter = new Mock<IElectStateFilter>();
            filter.Setup(x => x.OnStateElection(It.IsNotNull<ElectStateContext>()))
                .Callback((ElectStateContext x) => x.CandidateState = newState.Object);

            var context = CreateContext();

            // Act
            var electedState = context.ElectState(new[] { filter.Object });

            // Assert
            Assert.Same(newState.Object, electedState);

            _transaction.Verify(x => x.AddJobState(JobId, _candidateState.Object));
            _transaction.Verify(x => x.Dispose());
        }

        private ElectStateContext CreateContext()
        {
            return new ElectStateContext(
                _stateContext.Object,
                _candidateState.Object,
                "State");
        }
    }
}
