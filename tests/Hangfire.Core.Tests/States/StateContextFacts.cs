using System;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateContextFacts
    {
        private const string JobId = "job";

        private readonly Job _job;
        private readonly DateTime _createdAt;
        private readonly Mock<IStateMachine> _stateMachine;

        public StateContextFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine());
            _createdAt = new DateTime(2012, 12, 12);
            _stateMachine = new Mock<IStateMachine>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(null, _job, _createdAt, _stateMachine.Object));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(String.Empty, _job, _createdAt, _stateMachine.Object));

            Assert.Equal("jobId", exception.ParamName);
        }
        
        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenJobIsNull()
        {
            Assert.DoesNotThrow(() => new StateContext(JobId, null, _createdAt, _stateMachine.Object));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(JobId, _job, _createdAt, null));

            Assert.Equal("stateMachine", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = CreateContext();

            Assert.Equal(JobId, context.JobId);
            Assert.Equal(_createdAt, context.CreatedAt);
            Assert.Same(_job, context.Job);
            Assert.Same(_stateMachine.Object, context.StateMachine);
        }

        [Fact]
        public void CopyCtor_CopiesAllProperties()
        {
            var context = CreateContext();
            var contextCopy = new StateContext(context);

            Assert.Equal(context.JobId, contextCopy.JobId);
            Assert.Equal(context.CreatedAt, contextCopy.CreatedAt);
            Assert.Same(context.Job, contextCopy.Job);
            Assert.Same(context.StateMachine, contextCopy.StateMachine);
        }

        private StateContext CreateContext()
        {
            return new StateContext(JobId, _job, _createdAt, _stateMachine.Object);
        }
    }
}
