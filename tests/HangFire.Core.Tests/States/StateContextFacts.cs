using System;
using HangFire.Common;
using HangFire.Common.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateContextFacts
    {
        private readonly Job _job;

        public StateContextFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StateContext(null, _job));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StateContext(String.Empty, _job));
        }

        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenMethodDataIsNull()
        {
            Assert.DoesNotThrow(() => new StateContext("1", null));
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = new StateContext("1", _job);
            Assert.Equal("1", context.JobId);
            Assert.Same(_job, context.Job);
        }
    }
}
