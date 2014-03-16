using System;
using HangFire.Common;
using HangFire.Common.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateContextFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StateContext(null, new Mock<JobMethod>().Object));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StateContext(String.Empty, new Mock<JobMethod>().Object));
        }

        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenJobMethodIsNull()
        {
            Assert.DoesNotThrow(() => new StateContext("1", null));
        }
    }
}
