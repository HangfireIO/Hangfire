using System;
using HangFire.Common;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Common
{
    public class JobFilterAttributeFacts
    {
        [Fact]
        public void SetOrder_ThrowsAnException_WhenValueIsLessThanDefaultOrder()
        {
            var filterAttribute = new Mock<JobFilterAttribute>() { CallBase = true };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => filterAttribute.Object.Order = -2);
        }
    }
}
