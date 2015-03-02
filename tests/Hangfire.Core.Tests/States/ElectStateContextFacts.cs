﻿using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ElectStateContextFacts
    {
        private const string JobId = "1";
        private readonly StateContextMock _stateContext;
        private readonly Mock<IState> _candidateState;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IStateMachine> _stateMachine;

        public ElectStateContextFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _stateMachine = new Mock<IStateMachine>();

            _stateContext = new StateContextMock();
            _stateContext.JobIdValue = JobId;

            _candidateState = new Mock<IState>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _stateContext.Object,
                    null,
                    _stateMachine.Object,
                    _candidateState.Object,
                    null));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _stateContext.Object,
                    _connection.Object,
                    null,
                    _candidateState.Object,
                    null));

            Assert.Equal("stateMachine", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCandidateStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(
                    _stateContext.Object,
                    _connection.Object,
                    _stateMachine.Object,
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

            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_stateMachine.Object, context.StateMachine);
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

        private ElectStateContext CreateContext()
        {
            return new ElectStateContext(
                _stateContext.Object,
                _connection.Object,
                _stateMachine.Object,
                _candidateState.Object,
                "State");
        }
    }
}
