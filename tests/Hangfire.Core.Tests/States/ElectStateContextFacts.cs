using System;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;
#pragma warning disable 618

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.States
{
    public class ElectStateContextFacts
    {
        private readonly ApplyStateContextMock _applyContext;

        public ElectStateContextFacts()
        {
            _applyContext = new ApplyStateContextMock { OldStateName = "State" };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenApplyContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ElectStateContext(null));

            Assert.Equal("applyContext", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetAllProperties()
        {
            var context = CreateContext();
            
            Assert.Same(_applyContext.Connection.Object, context.Connection);
            Assert.Same(_applyContext.Transaction.Object, context.Transaction);
            Assert.Same(_applyContext.BackgroundJob.Object, context.BackgroundJob);
            Assert.Same(_applyContext.NewState.Object, context.CandidateState);
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

            Assert.Contains(_applyContext.NewState.Object, context.TraversedStates);
        }

        [Fact]
        public void SetJobParameter_CallsTheCorrespondingMethod_WithJsonEncodedValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", "Value");

            _applyContext.Connection.Verify(x => x.SetJobParameter(
                _applyContext.BackgroundJob.Id, "Name", JobHelper.ToJson("Value")));
        }

        [Fact]
        public void SetJobParameter_CanReceiveNullValue()
        {
            var context = CreateContext();

            context.SetJobParameter("Name", (string)null);

            _applyContext.Connection.Verify(x => x.SetJobParameter(
                _applyContext.BackgroundJob.Id, "Name", JobHelper.ToJson(null)));
        }

        [Fact]
        public void GetJobParameter_CallsTheCorrespondingMethod_WithJsonDecodedValue()
        {
            var context = CreateContext();
            _applyContext.Connection.Setup(x => x.GetJobParameter(_applyContext.BackgroundJob.Id, "Name"))
                .Returns(JobHelper.ToJson("Value"));

            var value = context.GetJobParameter<string>("Name");

            Assert.Equal("Value", value);
        }

        [Fact]
        public void GetJobParameter_ReturnsDefaultValue_WhenNoValueProvided()
        {
            var context = CreateContext();
            _applyContext.Connection.Setup(x => x.GetJobParameter("1", "Value"))
                .Returns(JobHelper.ToJson(null));

            var value = context.GetJobParameter<int>("Name");

            Assert.Equal(default(int), value);
        }

        private ElectStateContext CreateContext()
        {
            return new ElectStateContext(_applyContext.Object);
        }
    }
}
