using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Moq.Sequences;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class CoreStateMachineFacts
    {
        private const string OldStateName = "OldState";
        private const string StateName = "State";
        private const string JobId = "job";

        private readonly StateHandlerCollection _handlers = new StateHandlerCollection();
        private readonly Func<JobStorage, StateHandlerCollection> _stateHandlersThunk;
        
        private readonly ApplyStateContextMock _applyContext;

        public CoreStateMachineFacts()
        {
            _stateHandlersThunk = storage => _handlers;
            
            var backgroundJob = new BackgroundJobMock { Id = JobId };
            _applyContext = new ApplyStateContextMock
            {
                BackgroundJob = backgroundJob,
                OldStateName = OldStateName
            };

            _applyContext.NewState.Setup(x => x.Name).Returns(StateName);
        }

        [Fact]
        public void ApplyState_SetsTheNewState_ForABackgroundJob()
        {
            var stateMachine = CreateStateMachine();

            stateMachine.ApplyState(_applyContext.Object);

            _applyContext.Transaction.Verify(x => x.SetJobState(JobId, _applyContext.NewState.Object));
        }

        [Fact]
        public void ApplyState_SetsJobExpiration_IfStateIsFinal()
        {
            _applyContext.NewState.Setup(x => x.IsFinal).Returns(true);
            var stateMachine = CreateStateMachine();

            stateMachine.ApplyState(_applyContext.Object);

            _applyContext.Transaction.Verify(x => x.ExpireJob(JobId, _applyContext.JobExpirationTimeout));
        }

        [Fact]
        public void ApplyState_PersistTheJob_IfStateIsNotFinal()
        {
            _applyContext.NewState.Setup(x => x.IsFinal).Returns(false);
            var stateMachine = CreateStateMachine();

            stateMachine.ApplyState(_applyContext.Object);

            _applyContext.Transaction.Verify(x => x.PersistJob(JobId));
        }

        [Fact, Sequence]
        public void ApplyState_CallsUnapplyHandlers_BeforeSettingTheState()
        {
            // Arrange
            var handler1 = CreateStateHandler(OldStateName);
            var handler2 = CreateStateHandler(OldStateName);

            handler1
                .Setup(x => x.Unapply(_applyContext.Object, _applyContext.Transaction.Object))
                .InSequence();

            handler2
                .Setup(x => x.Unapply(_applyContext.Object, _applyContext.Transaction.Object))
                .InSequence();

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_applyContext.Object);

            // Assert - Sequence
        }

        [Fact]
        public void ApplyState_DoesNotCallUnapplyHandlers_ForDifferentStates()
        {
            // Arrange
            var handler = CreateStateHandler(StateName);
            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_applyContext.Object);

            // Assert
            handler.Verify(
                x => x.Unapply(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()),
                Times.Never);
        }

        [Fact, Sequence]
        public void ApplyState_ShouldCallApplyHandlers_AfterSettingTheState()
        {
            // Arrange
            var handler1 = CreateStateHandler(StateName);
            var handler2 = CreateStateHandler(StateName);

            _applyContext.Transaction
                .Setup(x => x.SetJobState(It.IsAny<string>(), It.IsAny<IState>()))
                .InSequence();

            handler1.Setup(x => x.Apply(_applyContext.Object, _applyContext.Transaction.Object))
                .InSequence();
            handler2.Setup(x => x.Apply(_applyContext.Object, _applyContext.Transaction.Object))
                .InSequence();

            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_applyContext.Object);

            // Assert - Sequence
        }

        [Fact]
        public void ApplyState_DoesNotCallApplyHandlers_ForDifferentStates()
        {
            // Arrange
            var handler = CreateStateHandler(OldStateName);
            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ApplyState(_applyContext.Object);

            // Assert
            handler.Verify(
                x => x.Apply(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()),
                Times.Never);
        }

        private CoreStateMachine CreateStateMachine()
        {
            return new CoreStateMachine(_stateHandlersThunk);
        }

        private Mock<IStateHandler> CreateStateHandler(string stateName)
        {
            var handler = new Mock<IStateHandler>();
            handler.Setup(x => x.StateName).Returns(stateName);

            _handlers.AddHandler(handler.Object);
            return handler;
        }
    }
}
