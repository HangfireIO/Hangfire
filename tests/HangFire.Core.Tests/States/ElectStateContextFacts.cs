using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ElectStateContextFacts
    {
        private const string JobId = "1";
        private readonly StateContext _stateContext;
        private readonly Mock<State> _candidateStateMock;
        private readonly Mock<IStorageConnection> _connectionMock;

        public ElectStateContextFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());
            _stateContext = new StateContext(JobId, job);
            _candidateStateMock = new Mock<State>();
            _connectionMock = new Mock<IStorageConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCandidateStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _stateContext,
                    null,
                    null,
                    _connectionMock.Object));

            Assert.Equal("candidateState", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _stateContext,
                    _candidateStateMock.Object,
                    null,
                    null));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetAllProperties()
        {
            var context = CreateContext();

            Assert.Equal(_stateContext.JobId, context.JobId);
            Assert.Equal(_stateContext.Job, context.Job);

            Assert.Same(_candidateStateMock.Object, context.CandidateState);
            Assert.Equal("State", context.CurrentState);
            Assert.Same(_connectionMock.Object, context.Connection);
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
            var newState = new Mock<State>();

            context.CandidateState = newState.Object;

            Assert.Same(newState.Object, context.CandidateState);
        }

        [Fact]
        public void SetJobParameter_CallsTheCorrespondingMethod_WithJsonEncodedValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", "Value");

            _connectionMock.Verify(x => x.SetJobParameter(
                JobId, "Name", JobHelper.ToJson("Value")));
        }

        [Fact]
        public void SetJobParameter_CanReceiveNullValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", (string)null);

            _connectionMock.Verify(x => x.SetJobParameter(
                JobId, "Name", JobHelper.ToJson(null)));
        }

        [Fact]
        public void GetJobParameter_CallsTheCorrespondingMethod_WithJsonDecodedValue()
        {
            var context = CreateContext();
            _connectionMock.Setup(x => x.GetJobParameter("1", "Name"))
                .Returns(JobHelper.ToJson("Value"));

            var value = context.GetJobParameter<string>("Name");

            Assert.Equal("Value", value);
        }

        [Fact]
        public void GetJobParameter_ReturnsDefaultValue_WhenNoValueProvided()
        {
            var context = CreateContext();
            _connectionMock.Setup(x => x.GetJobParameter("1", "Value"))
                .Returns(JobHelper.ToJson(null));

            var value = context.GetJobParameter<int>("Name");

            Assert.Equal(default(int), value);
        }

        private ElectStateContext CreateContext()
        {
            return new ElectStateContext(
                _stateContext,
                _candidateStateMock.Object,
                "State",
                _connectionMock.Object);
        }
    }
}
