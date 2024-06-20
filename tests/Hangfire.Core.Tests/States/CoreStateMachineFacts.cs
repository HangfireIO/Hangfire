using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Dictionary<string, List<IStateHandler>> _handlers = new Dictionary<string, List<IStateHandler>>();
        private readonly Func<JobStorage, string, CoreStateMachine.StateHandlersCollection> _stateHandlersThunk;
        
        private readonly ApplyStateContextMock _applyContext;

        public CoreStateMachineFacts()
        {
            _stateHandlersThunk = (storage, stateName) => new CoreStateMachine.StateHandlersCollection(
                _handlers.TryGetValue(stateName, out var handlers) ? handlers.ToArray() : Enumerable.Empty<IStateHandler>(),
                Enumerable.Empty<IStateHandler>(),
                stateName);
            
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

            if (!_handlers.ContainsKey(stateName)) _handlers.Add(stateName, new List<IStateHandler>());
            _handlers[stateName].Add(handler.Object);
            return handler;
        }
    }
}
